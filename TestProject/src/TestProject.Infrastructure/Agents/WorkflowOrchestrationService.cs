using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Agents;

/// <summary>
/// Service that manages ETW detector workflow execution using Microsoft Agent Framework
/// </summary>
public class WorkflowOrchestrationService(
  ETWDetectorWorkflow workflowFactory,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<WorkflowOrchestrationService> logger) : IWorkflowOrchestrationService
{
  private readonly ConcurrentDictionary<Guid, List<WorkflowEvent>> _eventBuffers = new();
  private readonly ConcurrentDictionary<Guid, bool> _workflowCompleted = new();
  private readonly ConcurrentDictionary<Guid, Guid> _workflowToThreadMapping = new();

  public async Task<Guid> StartWorkflowAsync(string userId, string etwDetails, Guid? existingThreadId = null, CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Starting ETW detector workflow for user {UserId}", userId);

    // Use existing thread or create new one
    Guid threadId;
    if (existingThreadId.HasValue)
    {
      threadId = existingThreadId.Value;
      logger.LogInformation("Using existing thread {ThreadId}", threadId);
    }
    else
    {
      var newConversation = await conversationService.CreateThreadAsync(userId, cancellationToken);
      threadId = newConversation.ThreadId;
      logger.LogInformation("Created new thread {ThreadId}", threadId);
    }

    var workflowId = Guid.NewGuid();

    // Store workflow ID in conversation state for SignalR broadcasting
    var conversationState = await conversationService.GetThreadStateAsync(threadId, cancellationToken);
    if (conversationState != null)
    {
      conversationState.StepData["WorkflowId"] = workflowId.ToString();
      await conversationService.UpdateThreadStateAsync(conversationState, cancellationToken);
    }

    // Send workflow start message
    await SendConversationMessageAsync(threadId, "🚀 Starting the agent workflow to create your ETW detector...");

    var workflow = workflowFactory.BuildWorkflow();

    // Map workflow ID to thread ID
    _workflowToThreadMapping[workflowId] = threadId;

    // Set current workflow context so executors can access thread ID
    contextProvider.SetCurrentWorkflow(workflowId, threadId);
    logger.LogInformation("Set current workflow {WorkflowId} to thread {ThreadId}", workflowId, threadId);

    // Create event buffer for this workflow
    var eventBuffer = new List<WorkflowEvent>();
    _eventBuffers[workflowId] = eventBuffer;
    _workflowCompleted[workflowId] = false;

    // Create initial message for the workflow (ETWInput for AI agent)
    var initialMessage = new ETWInput(userId, etwDetails);

    // Start workflow execution in background
    _ = Task.Run(async () =>
    {
      try
      {
        logger.LogInformation("Executing workflow {WorkflowId} with thread {ThreadId}", workflowId, threadId);

        // Execute the workflow using InProcessExecution
        var run = await InProcessExecution.StreamAsync(workflow, initialMessage);

        logger.LogInformation("Workflow run started for workflow {WorkflowId}", workflowId);

        // Collect all events in the buffer
        await foreach (var evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
          logger.LogInformation("Workflow {WorkflowId} event: {EventType}", workflowId, evt.GetType().Name);

          // Add event to buffer (thread-safe with lock)
          lock (eventBuffer)
          {
            eventBuffer.Add(evt);
          }

          if (evt is WorkflowOutputEvent output)
          {
            logger.LogInformation("Workflow {WorkflowId} completed with output: {Output}",
              workflowId, output.Data?.ToString());
            await SendConversationMessageAsync(threadId, "✓ Workflow completed successfully!");
          }
          else if (evt is WorkflowErrorEvent errorEvent)
          {
            logger.LogError("Workflow {WorkflowId} error occurred", workflowId);
            await SendConversationMessageAsync(threadId, $"⚠️ An error occurred: {errorEvent.GetType().Name}", ConversationMessageType.Error);
          }
        }

        logger.LogInformation("Workflow {WorkflowId} completed", workflowId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
        await SendConversationMessageAsync(threadId, $"⚠️ Workflow failed: {ex.Message}", ConversationMessageType.Error);
      }
      finally
      {
        // Mark workflow as completed
        _workflowCompleted[workflowId] = true;
      }
    }, cancellationToken);

    return workflowId;
  }

  private async Task SendConversationMessageAsync(
    Guid threadId,
    string content,
    ConversationMessageType type = ConversationMessageType.AgentMessage)
  {
    var message = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = type,
      Content = content
    };
    await conversationService.AddMessageAsync(threadId, message, CancellationToken.None);
  }

  public Task<string?> GetWorkflowResultAsync(Guid workflowId, CancellationToken cancellationToken = default)
  {
    // Simplified for now - in a real implementation, would track results
    return Task.FromResult<string?>(null);
  }

  public Guid? GetThreadIdForWorkflow(Guid workflowId)
  {
    return _workflowToThreadMapping.TryGetValue(workflowId, out var threadId) ? threadId : null;
  }

  public async IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(
    Guid workflowId,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    if (!_eventBuffers.TryGetValue(workflowId, out var eventBuffer))
    {
      logger.LogWarning("No event buffer found for workflow {WorkflowId}", workflowId);
      yield break;
    }

    logger.LogInformation("Streaming events for workflow {WorkflowId}", workflowId);

    var lastEventIndex = 0;

    // Poll for new events until workflow completes
    while (!cancellationToken.IsCancellationRequested)
    {
      List<WorkflowEvent> eventsToSend;

      // Get new events from buffer (thread-safe)
      lock (eventBuffer)
      {
        var currentCount = eventBuffer.Count;
        if (currentCount > lastEventIndex)
        {
          eventsToSend = eventBuffer.Skip(lastEventIndex).ToList();
          lastEventIndex = currentCount;
        }
        else
        {
          eventsToSend = new List<WorkflowEvent>();
        }
      }

      // Yield all new events
      foreach (var evt in eventsToSend)
      {
        yield return evt;
      }

      // Check if workflow is complete
      if (_workflowCompleted.TryGetValue(workflowId, out var isCompleted) && isCompleted)
      {
        // Send any remaining events that might have been added
        lock (eventBuffer)
        {
          if (eventBuffer.Count > lastEventIndex)
          {
            foreach (var evt in eventBuffer.Skip(lastEventIndex))
            {
              yield return evt;
            }
          }
        }
        break;
      }

      // Wait a bit before polling again (avoid tight loop)
      await Task.Delay(50, cancellationToken);
    }

    logger.LogInformation("Finished streaming events for workflow {WorkflowId}", workflowId);
  }
}
