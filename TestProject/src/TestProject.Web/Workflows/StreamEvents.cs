using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using TestProject.Core.Agents;

namespace TestProject.Web.Workflows;

/// <summary>
/// SSE endpoint for streaming workflow events in real-time
/// </summary>
public class StreamEventsRequest
{
  public Guid WorkflowId { get; set; }
}

public class StreamEvents(IWorkflowOrchestrationService workflowService, ILogger<StreamEvents> logger)
  : Endpoint<StreamEventsRequest>
{
  public override void Configure()
  {
    Get("/api/workflows/{workflowId}/stream");
    AllowAnonymous();
  }

  public override async Task HandleAsync(StreamEventsRequest req, CancellationToken ct)
  {
    logger.LogInformation("Starting SSE stream for workflow {WorkflowId}", req.WorkflowId);

    // Set response headers for Server-Sent Events
    HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
    HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
    HttpContext.Response.Headers.Append("Connection", "keep-alive");

    try
    {
      // Stream workflow events to the client
      await foreach (var evt in workflowService.StreamWorkflowEventsAsync(req.WorkflowId, ct))
      {
        var eventType = evt.GetType().Name;
        object data;

        if (evt is WorkflowOutputEvent output)
        {
          data = new { output = output.Data?.ToString() };
        }
        else if (evt is WorkflowStartedEvent)
        {
          data = new { message = "Workflow started" };
        }
        else if (evt is WorkflowErrorEvent)
        {
          data = new { message = "Workflow error occurred" };
        }
        else
        {
          // For other event types (executor events, etc.), use reflection to get executor info if available
          var executorProp = evt.GetType().GetProperty("ExecutorId");
          if (executorProp != null)
          {
            var executorId = executorProp.GetValue(evt)?.ToString();
            data = new { executor = executorId };
          }
          else
          {
            data = new { message = $"Event: {eventType}" };
          }
        }

        var eventData = new
        {
          type = eventType,
          timestamp = DateTime.UtcNow,
          data = data
        };

        var json = JsonSerializer.Serialize(eventData);

        // Write SSE formatted message
        await HttpContext.Response.WriteAsync($"data: {json}\n\n", ct);
        await HttpContext.Response.Body.FlushAsync(ct);

        logger.LogDebug("Sent event {EventType} for workflow {WorkflowId}", evt.GetType().Name, req.WorkflowId);
      }

      logger.LogInformation("Completed SSE stream for workflow {WorkflowId}", req.WorkflowId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error streaming events for workflow {WorkflowId}", req.WorkflowId);

      var errorData = JsonSerializer.Serialize(new
      {
        type = "error",
        message = ex.Message
      });

      await HttpContext.Response.WriteAsync($"data: {errorData}\n\n", ct);
      await HttpContext.Response.Body.FlushAsync(ct);
    }
  }
}
