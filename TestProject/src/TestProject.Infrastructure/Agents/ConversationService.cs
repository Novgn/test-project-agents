using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Agents;

/// <summary>
/// Service that manages conversation threads for conversational AI workflow
/// </summary>
public class ConversationService(
  IHubContext<ConversationHub> hubContext,
  ILogger<ConversationService> logger) : IConversationService
{
  private readonly ConcurrentDictionary<Guid, ConversationState> _conversationStates = new();

  public Task<ConversationState> CreateThreadAsync(string userId, CancellationToken cancellationToken = default)
  {
    var threadId = Guid.NewGuid();
    var conversationState = new ConversationState
    {
      ThreadId = threadId,
      UserId = userId,
      CurrentStep = WorkflowPhase.Initial
    };

    _conversationStates[threadId] = conversationState;

    logger.LogInformation("Created conversation thread {ThreadId} for user {UserId}", threadId, userId);
    return Task.FromResult(conversationState);
  }

  public Task<ConversationState?> GetThreadStateAsync(Guid threadId, CancellationToken cancellationToken = default)
  {
    _conversationStates.TryGetValue(threadId, out var state);
    return Task.FromResult(state);
  }

  public Task UpdateThreadStateAsync(ConversationState state, CancellationToken cancellationToken = default)
  {
    state.UpdatedAt = DateTime.UtcNow;
    _conversationStates[state.ThreadId] = state;

    logger.LogInformation("Updated thread {ThreadId} state to step {Step}",
      state.ThreadId, state.CurrentStep);

    return Task.CompletedTask;
  }

  public async Task AddMessageAsync(Guid threadId, ConversationMessage message, CancellationToken cancellationToken = default)
  {
    if (!_conversationStates.TryGetValue(threadId, out var state))
    {
      logger.LogWarning("Attempted to add message to non-existent thread {ThreadId}", threadId);
      return;
    }

    // Add to conversation history with role prefix
    var historyEntry = message.Type == ConversationMessageType.UserResponse
      ? $"User: {message.Content}"
      : $"Agent: {message.Content}";
    state.ConversationHistory.Add(historyEntry);
    state.UpdatedAt = DateTime.UtcNow;

    // Broadcast message via SignalR to the workflow group
    var workflowId = state.StepData.ContainsKey("WorkflowId")
      ? state.StepData["WorkflowId"].ToString()
      : threadId.ToString();

    await hubContext.Clients.Group(workflowId!).SendAsync("ReceiveMessage", new
    {
      id = message.Id,
      type = message.Type.ToString(),
      content = message.Content,
      data = message.Data,
      timestamp = message.Timestamp
    }, cancellationToken);

    logger.LogDebug("Broadcast message {MessageId} of type {MessageType} to workflow {WorkflowId}",
      message.Id, message.Type, workflowId);
  }

  public async Task<ApprovalRequest> RequestApprovalAsync(
    Guid threadId,
    ApprovalRequest request,
    CancellationToken cancellationToken = default)
  {
    if (!_conversationStates.TryGetValue(threadId, out var state))
    {
      throw new InvalidOperationException($"Thread {threadId} not found");
    }

    // Add to pending approvals
    state.PendingApprovals.Add(request);
    state.UpdatedAt = DateTime.UtcNow;

    // Send approval request as a message
    var message = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = ConversationMessageType.ApprovalRequest,
      Content = request.Question,
      Data = request
    };

    await AddMessageAsync(threadId, message, cancellationToken);

    logger.LogInformation("Requested approval {ApprovalId} for thread {ThreadId} at step {Step}",
      request.Id, threadId, request.Step);

    return request;
  }

  public async Task<bool> ProcessApprovalAsync(
    Guid threadId,
    ApprovalResponse response,
    CancellationToken cancellationToken = default)
  {
    if (!_conversationStates.TryGetValue(threadId, out var state))
    {
      throw new InvalidOperationException($"Thread {threadId} not found");
    }

    // Find and remove the pending approval
    var approval = state.PendingApprovals.FirstOrDefault(a => a.Id == response.Id);
    if (approval == null)
    {
      logger.LogWarning("Approval {ApprovalId} not found in thread {ThreadId}", response.Id, threadId);
      return false;
    }

    state.PendingApprovals.Remove(approval);
    state.UpdatedAt = DateTime.UtcNow;

    // Add response as a message
    var message = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = ConversationMessageType.UserResponse,
      Content = response.Approved
        ? $"Approved: {response.Feedback ?? "Proceeding with the action"}"
        : $"Rejected: {response.Feedback ?? "Not proceeding"}",
      Data = response
    };

    await AddMessageAsync(threadId, message, cancellationToken);

    logger.LogInformation("Processed approval {ApprovalId} for thread {ThreadId}: {Approved}",
      response.Id, threadId, response.Approved);

    return response.Approved;
  }

  public IAsyncEnumerable<ConversationMessage> StreamMessagesAsync(
    Guid threadId,
    CancellationToken cancellationToken = default)
  {
    // SignalR handles streaming now, this method is no longer used
    throw new NotImplementedException("Use SignalR hub for streaming messages");
  }
}
