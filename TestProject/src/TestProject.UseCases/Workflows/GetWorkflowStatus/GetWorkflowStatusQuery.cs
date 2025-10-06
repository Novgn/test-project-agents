namespace TestProject.UseCases.Workflows.GetWorkflowStatus;

public record GetWorkflowStatusQuery(Guid WorkflowRunId) : IRequest<Result<WorkflowStatusDTO>>;

public record WorkflowStatusDTO(
  Guid Id,
  string UserId,
  string ETWDetails,
  WorkflowStatus Status,
  string? CurrentStepName,
  DateTime StartedAt,
  DateTime? CompletedAt,
  List<WorkflowStepDTO> Steps
);

public record WorkflowStepDTO(
  string StepName,
  WorkflowStepType StepType,
  WorkflowStatus Status,
  string? Output,
  string? ErrorMessage,
  DateTime StartedAt,
  DateTime? CompletedAt
);

public class GetWorkflowStatusHandler(
  IReadRepository<WorkflowRun> repository,
  ILogger<GetWorkflowStatusHandler> logger)
  : IRequestHandler<GetWorkflowStatusQuery, Result<WorkflowStatusDTO>>
{
  public async Task<Result<WorkflowStatusDTO>> Handle(GetWorkflowStatusQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Getting status for workflow {WorkflowId}", request.WorkflowRunId);

    var spec = new GetWorkflowByIdSpec(request.WorkflowRunId);
    var workflowRun = await repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (workflowRun == null)
    {
      return Result<WorkflowStatusDTO>.NotFound($"Workflow {request.WorkflowRunId} not found");
    }

    var dto = new WorkflowStatusDTO(
      workflowRun.Id,
      workflowRun.UserId,
      workflowRun.ETWDetails,
      workflowRun.Status,
      workflowRun.CurrentStepName,
      workflowRun.StartedAt,
      workflowRun.CompletedAt,
      workflowRun.Steps.Select(s => new WorkflowStepDTO(
        s.StepName,
        s.StepType,
        s.Status,
        s.Output,
        s.ErrorMessage,
        s.StartedAt,
        s.CompletedAt
      )).ToList()
    );

    return Result<WorkflowStatusDTO>.Success(dto);
  }
}
