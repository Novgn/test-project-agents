using System.Threading.Channels;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TestProject.Core.Agents;

namespace TestProject.Infrastructure.Agents;

/// <summary>
/// Service that manages ETW detector workflow execution using Microsoft Agent Framework
/// </summary>
public class WorkflowOrchestrationService(
  ETWDetectorWorkflow workflowFactory,
  ILogger<WorkflowOrchestrationService> logger) : IWorkflowOrchestrationService
{
  private readonly Dictionary<Guid, Channel<WorkflowEvent>> _eventChannels = new();

  public Task<Guid> StartWorkflowAsync(string userId, string etwDetails, CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Starting ETW detector workflow for user {UserId}", userId);

    var workflowId = Guid.NewGuid();
    var workflow = workflowFactory.BuildWorkflow();

    // Create a channel for streaming events to subscribers
    var eventChannel = Channel.CreateUnbounded<WorkflowEvent>();
    _eventChannels[workflowId] = eventChannel;

    // Create initial message for the workflow
    var initialMessage = new ChatMessage(ChatRole.User, etwDetails);

    // Start workflow execution in background
    _ = Task.Run(async () =>
    {
      try
      {
        logger.LogInformation("Executing workflow {WorkflowId}", workflowId);

        // Execute the workflow using InProcessExecution
        var run = await InProcessExecution.StreamAsync(workflow, initialMessage);

        // Stream all events to the channel
        await foreach (var evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
          logger.LogInformation("Workflow {WorkflowId} event: {EventType}", workflowId, evt.GetType().Name);

          // Write event to channel for subscribers
          await eventChannel.Writer.WriteAsync(evt, cancellationToken);

          if (evt is WorkflowOutputEvent output)
          {
            logger.LogInformation("Workflow {WorkflowId} completed with output: {Output}",
              workflowId, output.Data?.ToString());
          }
          else if (evt is WorkflowErrorEvent)
          {
            logger.LogError("Workflow {WorkflowId} error occurred", workflowId);
          }
        }

        logger.LogInformation("Workflow {WorkflowId} completed", workflowId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
      }
      finally
      {
        // Complete the channel when workflow is done
        eventChannel.Writer.Complete();
      }
    }, cancellationToken);

    return Task.FromResult(workflowId);
  }

  public Task<string?> GetWorkflowResultAsync(Guid workflowId, CancellationToken cancellationToken = default)
  {
    // Simplified for now - in a real implementation, would track results
    return Task.FromResult<string?>(null);
  }

  public async IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(
    Guid workflowId,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    if (!_eventChannels.TryGetValue(workflowId, out var channel))
    {
      logger.LogWarning("No event channel found for workflow {WorkflowId}", workflowId);
      yield break;
    }

    logger.LogInformation("Streaming events for workflow {WorkflowId}", workflowId);

    // Stream events from the channel to the client
    await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
    {
      yield return evt;
    }

    logger.LogInformation("Finished streaming events for workflow {WorkflowId}", workflowId);
  }
}
