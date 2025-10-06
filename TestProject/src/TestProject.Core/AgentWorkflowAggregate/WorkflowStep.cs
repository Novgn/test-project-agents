namespace TestProject.Core.AgentWorkflowAggregate;

public class WorkflowStep : EntityBase<Guid>
{
  public Guid WorkflowRunId { get; private set; }
  public WorkflowStepType StepType { get; private set; }
  public string StepName { get; private set; } = string.Empty;
  public string Description { get; private set; } = string.Empty;
  public WorkflowStatus Status { get; private set; }
  public string? Input { get; private set; }
  public string? Output { get; private set; }
  public string? ErrorMessage { get; private set; }
  public DateTime StartedAt { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public int Sequence { get; private set; }

  private WorkflowStep() { } // EF Core

  public WorkflowStep(
    Guid workflowRunId,
    WorkflowStepType stepType,
    string stepName,
    string description,
    int sequence)
  {
    WorkflowRunId = Guard.Against.Default(workflowRunId, nameof(workflowRunId));
    StepType = stepType;
    StepName = Guard.Against.NullOrEmpty(stepName, nameof(stepName));
    Description = Guard.Against.NullOrEmpty(description, nameof(description));
    Sequence = Guard.Against.Negative(sequence, nameof(sequence));
    Status = WorkflowStatus.Pending;
    StartedAt = DateTime.UtcNow;
  }

  public void Start(string? input = null)
  {
    Status = WorkflowStatus.InProgress;
    Input = input;
    StartedAt = DateTime.UtcNow;
  }

  public void Complete(string? output = null)
  {
    Status = WorkflowStatus.Completed;
    Output = output;
    CompletedAt = DateTime.UtcNow;
  }

  public void Fail(string errorMessage)
  {
    Status = WorkflowStatus.Failed;
    ErrorMessage = Guard.Against.NullOrEmpty(errorMessage, nameof(errorMessage));
    CompletedAt = DateTime.UtcNow;
  }

  public void WaitForApproval()
  {
    Status = WorkflowStatus.WaitingForApproval;
  }

  public void Approve()
  {
    if (Status != WorkflowStatus.WaitingForApproval)
    {
      throw new InvalidOperationException("Cannot approve a step that is not waiting for approval");
    }
    Status = WorkflowStatus.Approved;
  }

  public void Reject(string reason)
  {
    if (Status != WorkflowStatus.WaitingForApproval)
    {
      throw new InvalidOperationException("Cannot reject a step that is not waiting for approval");
    }
    Status = WorkflowStatus.Rejected;
    ErrorMessage = reason;
    CompletedAt = DateTime.UtcNow;
  }
}
