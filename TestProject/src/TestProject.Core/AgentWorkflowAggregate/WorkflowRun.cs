namespace TestProject.Core.AgentWorkflowAggregate;

public class WorkflowRun : EntityBase<Guid>, IAggregateRoot
{
  private readonly List<WorkflowStep> _steps = new();

  public string UserId { get; private set; } = string.Empty;
  public string ETWDetails { get; private set; } = string.Empty;
  public WorkflowStatus Status { get; private set; }
  public DateTime StartedAt { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public string? CurrentStepName { get; private set; }
  public string? ErrorMessage { get; private set; }

  public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

  private WorkflowRun() { } // EF Core

  public WorkflowRun(string userId, string etwDetails)
  {
    Id = Guid.NewGuid();
    UserId = Guard.Against.NullOrEmpty(userId, nameof(userId));
    ETWDetails = Guard.Against.NullOrEmpty(etwDetails, nameof(etwDetails));
    Status = WorkflowStatus.Pending;
    StartedAt = DateTime.UtcNow;
    InitializeWorkflowSteps();
  }

  private void InitializeWorkflowSteps()
  {
    var stepDefinitions = new[]
    {
      (WorkflowStepType.AcceptUserInput, "Accept ETW Details", "Capture and validate user input for ETW detector"),
      (WorkflowStepType.QueryKusto, "Query Kusto", "Query Azure Kusto cluster to identify converters and detectors"),
      (WorkflowStepType.CreateBranch, "Create Azure Repo Branch", "Create new branch with necessary files and configurations"),
      (WorkflowStepType.AnalyzePRHistory, "Analyze PR History", "Review historical PRs for patterns and conventions"),
      (WorkflowStepType.GenerateDetectorCode, "Generate Detector Code", "Generate detector code based on templates and patterns"),
      (WorkflowStepType.CreatePR, "Create Pull Request", "Create PR with generated code and request user review"),
      (WorkflowStepType.WaitForPRApproval, "Wait for PR Approval", "Wait for user to approve changes"),
      (WorkflowStepType.MonitorDeployment, "Monitor Deployment", "Monitor deployment pipeline for merged PR"),
      (WorkflowStepType.FetchDetectorResults, "Fetch Detector Results", "Fetch and analyze detector results from Kusto"),
      (WorkflowStepType.AnalyzeResults, "Analyze Results", "Analyze detector performance and results"),
      (WorkflowStepType.CreateCustomerFacingPR, "Create Customer-Facing PR", "Create PR to make detector customer-facing")
    };

    for (int i = 0; i < stepDefinitions.Length; i++)
    {
      var (stepType, name, description) = stepDefinitions[i];
      var step = new WorkflowStep(Id, stepType, name, description, i + 1);
      _steps.Add(step);
    }
  }

  public void Start()
  {
    Status = WorkflowStatus.InProgress;
    var firstStep = _steps.FirstOrDefault();
    if (firstStep != null)
    {
      firstStep.Start(ETWDetails);
      CurrentStepName = firstStep.StepName;
    }
  }

  public WorkflowStep? GetCurrentStep()
  {
    return _steps
      .Where(s => s.Status == WorkflowStatus.InProgress || s.Status == WorkflowStatus.WaitingForApproval)
      .OrderBy(s => s.Sequence)
      .FirstOrDefault();
  }

  public WorkflowStep? GetNextPendingStep()
  {
    return _steps
      .Where(s => s.Status == WorkflowStatus.Pending)
      .OrderBy(s => s.Sequence)
      .FirstOrDefault();
  }

  public void CompleteCurrentStep(string? output = null)
  {
    var currentStep = GetCurrentStep();
    if (currentStep == null)
    {
      throw new InvalidOperationException("No current step to complete");
    }

    currentStep.Complete(output);

    // Start next pending step if available
    var nextStep = GetNextPendingStep();
    if (nextStep != null)
    {
      nextStep.Start();
      CurrentStepName = nextStep.StepName;
    }
    else
    {
      // All steps completed
      Status = WorkflowStatus.Completed;
      CompletedAt = DateTime.UtcNow;
      CurrentStepName = null;
    }
  }

  public void FailCurrentStep(string errorMessage)
  {
    var currentStep = GetCurrentStep();
    if (currentStep == null)
    {
      throw new InvalidOperationException("No current step to fail");
    }

    currentStep.Fail(errorMessage);
    Status = WorkflowStatus.Failed;
    ErrorMessage = errorMessage;
    CompletedAt = DateTime.UtcNow;
  }

  public void RequestApproval()
  {
    var currentStep = GetCurrentStep();
    if (currentStep == null)
    {
      throw new InvalidOperationException("No current step to request approval for");
    }

    currentStep.WaitForApproval();
    Status = WorkflowStatus.WaitingForApproval;
  }

  public void ApproveCurrentStep()
  {
    var currentStep = GetCurrentStep();
    if (currentStep == null || currentStep.Status != WorkflowStatus.WaitingForApproval)
    {
      throw new InvalidOperationException("No step waiting for approval");
    }

    currentStep.Approve();
    CompleteCurrentStep();
  }

  public void RejectCurrentStep(string reason)
  {
    var currentStep = GetCurrentStep();
    if (currentStep == null || currentStep.Status != WorkflowStatus.WaitingForApproval)
    {
      throw new InvalidOperationException("No step waiting for approval");
    }

    currentStep.Reject(reason);
    Status = WorkflowStatus.Rejected;
    ErrorMessage = reason;
    CompletedAt = DateTime.UtcNow;
  }

  public void Cancel()
  {
    if (Status == WorkflowStatus.Completed || Status == WorkflowStatus.Failed)
    {
      throw new InvalidOperationException("Cannot cancel a completed or failed workflow");
    }

    Status = WorkflowStatus.Cancelled;
    CompletedAt = DateTime.UtcNow;
  }
}
