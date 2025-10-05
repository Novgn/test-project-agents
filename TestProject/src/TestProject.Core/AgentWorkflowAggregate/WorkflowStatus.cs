namespace TestProject.Core.AgentWorkflowAggregate;

public enum WorkflowStatus
{
  Pending = 0,
  InProgress = 1,
  WaitingForApproval = 2,
  Approved = 3,
  Rejected = 4,
  Completed = 5,
  Failed = 6,
  Cancelled = 7
}
