using TestProject.Core.AgentWorkflowAggregate;

namespace TestProject.Core.Interfaces;

/// <summary>
/// Manages conversation threads for the conversational AI workflow
/// </summary>
public interface IConversationService
{
  /// <summary>
  /// Creates a new conversation thread
  /// </summary>
  Task<ConversationState> CreateThreadAsync(string userId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets the current state of a conversation thread
  /// </summary>
  Task<ConversationState?> GetThreadStateAsync(Guid threadId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Updates the conversation state
  /// </summary>
  Task UpdateThreadStateAsync(ConversationState state, CancellationToken cancellationToken = default);

  /// <summary>
  /// Adds a message to the conversation history
  /// </summary>
  Task AddMessageAsync(Guid threadId, ConversationMessage message, CancellationToken cancellationToken = default);

  /// <summary>
  /// Adds an approval request and pauses workflow
  /// </summary>
  Task<ApprovalRequest> RequestApprovalAsync(Guid threadId, ApprovalRequest request, CancellationToken cancellationToken = default);

  /// <summary>
  /// Processes user's approval response and resumes workflow
  /// </summary>
  Task<bool> ProcessApprovalAsync(Guid threadId, ApprovalResponse response, CancellationToken cancellationToken = default);

  /// <summary>
  /// Streams conversation messages as they occur
  /// </summary>
  IAsyncEnumerable<ConversationMessage> StreamMessagesAsync(Guid threadId, CancellationToken cancellationToken = default);
}
