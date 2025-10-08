using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Web.Workflows;

/// <summary>
/// Endpoint for sending user approval responses
/// </summary>
public class SendApprovalRequest
{
  public Guid WorkflowId { get; set; }
  public required string ApprovalId { get; set; }
  public bool Approved { get; set; }
  public string? Feedback { get; set; }
}

public class SendApprovalResponse
{
  public bool Success { get; set; }
}

public class SendApproval(
  IWorkflowOrchestrationService workflowService,
  IConversationService conversationService,
  ILogger<SendApproval> logger)
  : Endpoint<SendApprovalRequest, SendApprovalResponse>
{
  public override void Configure()
  {
    Post("/api/workflows/{workflowId}/approve");
    AllowAnonymous();
    Options(x => x.RequireCors());
  }

  public override async Task HandleAsync(SendApprovalRequest req, CancellationToken ct)
  {
    logger.LogInformation("Processing approval {ApprovalId} for workflow {WorkflowId}: {Approved}",
      req.ApprovalId, req.WorkflowId, req.Approved);

    // Get thread ID from workflow
    var threadId = workflowService.GetThreadIdForWorkflow(req.WorkflowId);
    if (threadId == null)
    {
      logger.LogWarning("Workflow {WorkflowId} not found or no thread associated", req.WorkflowId);
      await SendAsync(new SendApprovalResponse { Success = false }, statusCode: 404, cancellation: ct);
      return;
    }

    // Create approval response
    var response = new ApprovalResponse
    {
      Id = req.ApprovalId,
      Approved = req.Approved,
      Feedback = req.Feedback
    };

    // Process the approval
    await conversationService.ProcessApprovalAsync(threadId.Value, response, ct);

    logger.LogInformation("Approval {ApprovalId} processed successfully", req.ApprovalId);
    await SendAsync(new SendApprovalResponse { Success = true }, cancellation: ct);
  }
}
