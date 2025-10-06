using TestProject.UseCases.Workflows.GetWorkflowStatus;

namespace TestProject.Web.Workflows;

public class GetStatusRequest
{
  public Guid WorkflowId { get; set; }
}

public class GetStatus(IMediator mediator) : Endpoint<GetStatusRequest, WorkflowStatusDTO>
{
  public override void Configure()
  {
    Get("/api/workflows/{workflowId}/status");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Get workflow status";
      s.Description = "Retrieves the current status and progress of a workflow";
    });
  }

  public override async Task HandleAsync(GetStatusRequest req, CancellationToken ct)
  {
    var query = new GetWorkflowStatusQuery(req.WorkflowId);
    var result = await mediator.Send(query, ct);

    if (result.IsSuccess)
    {
      await SendAsync(result.Value, cancellation: ct);
    }
    else
    {
      await SendNotFoundAsync(ct);
    }
  }
}
