namespace TestProject.UseCases.Workflows.StartWorkflow;

public record StartWorkflowCommand(string UserId, string ETWDetails) : IRequest<Result<Guid>>;

public class StartWorkflowHandler(
  IWorkflowOrchestrationService workflowService,
  ILogger<StartWorkflowHandler> logger)
  : IRequestHandler<StartWorkflowCommand, Result<Guid>>
{
  public async Task<Result<Guid>> Handle(StartWorkflowCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Starting ETW detector workflow for user {UserId} with ETW: {ETW}",
      request.UserId, request.ETWDetails);

    try
    {
      var workflowId = await workflowService.StartWorkflowAsync(
        request.UserId,
        request.ETWDetails,
        existingThreadId: null,
        cancellationToken);

      logger.LogInformation("Workflow {WorkflowId} started successfully using Microsoft Agent Framework", workflowId);

      return Result<Guid>.Success(workflowId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to start workflow for user {UserId}", request.UserId);
      return Result<Guid>.Error($"Failed to start workflow: {ex.Message}");
    }
  }
}
