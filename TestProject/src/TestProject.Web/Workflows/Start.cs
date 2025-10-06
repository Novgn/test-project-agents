using TestProject.UseCases.Workflows.StartWorkflow;

namespace TestProject.Web.Workflows;

public class StartWorkflowRequest
{
  public string UserId { get; set; } = string.Empty;
  public string ETWDetails { get; set; } = string.Empty;
}

public class StartWorkflowResponse
{
  public Guid WorkflowId { get; set; }
}

public class Start(IMediator mediator) : Endpoint<StartWorkflowRequest, StartWorkflowResponse>
{
  public override void Configure()
  {
    Post("/api/workflows/start");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Start a new agent workflow";
      s.Description = "Initiates an AI agent workflow to create an ETW detector";
    });
  }

  public override async Task HandleAsync(StartWorkflowRequest req, CancellationToken ct)
  {
    var command = new StartWorkflowCommand(req.UserId, req.ETWDetails);
    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendAsync(new StartWorkflowResponse { WorkflowId = result.Value }, cancellation: ct);
    }
    else
    {
      await SendErrorsAsync(cancellation: ct);
    }
  }
}
