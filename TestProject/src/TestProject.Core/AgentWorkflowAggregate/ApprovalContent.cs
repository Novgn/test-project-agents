namespace TestProject.Core.AgentWorkflowAggregate;

/// <summary>
/// Request sent to user for approval before proceeding with workflow step
/// </summary>
public record ApprovalRequest
{
  public required string Id { get; init; }
  public required string Question { get; init; }
  public required string Context { get; init; }
  public object? Data { get; init; }
  public required WorkflowPhase Step { get; init; }
  public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User's response to an approval request
/// </summary>
public record ApprovalResponse
{
  public required string Id { get; init; }
  public required bool Approved { get; init; }
  public string? Feedback { get; init; }
  public DateTime RespondedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the current phase in the conversational workflow
/// </summary>
public enum WorkflowPhase
{
  Initial,
  GatheringETWDetails,
  QueryingKusto,
  AnalyzingPRPatterns,
  CreatingBranch,
  GeneratingDetectorCode,
  CreatingPullRequest,
  MonitoringDeployment,
  AnalyzingDetectorResults,
  CreatingRolloutPR,
  Completed
}

/// <summary>
/// State of a conversation thread
/// </summary>
public record ConversationState
{
  public required Guid ThreadId { get; init; }
  public required string UserId { get; init; }
  public required WorkflowPhase CurrentStep { get; set; }
  public Dictionary<string, object> StepData { get; init; } = new();
  public List<ApprovalRequest> PendingApprovals { get; init; } = new();
  public List<string> ConversationHistory { get; init; } = new();
  public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
  public bool IsComplete { get; set; }
}

/// <summary>
/// Message sent to user during conversation
/// </summary>
public record ConversationMessage
{
  public required string Id { get; init; }
  public required ConversationMessageType Type { get; init; }
  public required string Content { get; init; }
  public object? Data { get; init; }
  public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Types of messages in the conversation
/// </summary>
public enum ConversationMessageType
{
  AgentMessage,
  AgentQuestion,
  ApprovalRequest,
  UserResponse,
  StepComplete,
  Error,
  SystemMessage
}
