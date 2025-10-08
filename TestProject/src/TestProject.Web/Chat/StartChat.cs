using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Web.Chat;

public class StartChatRequest
{
  public string UserId { get; set; } = string.Empty;
  public string InitialMessage { get; set; } = string.Empty;
}

public class StartChatResponse
{
  public Guid ThreadId { get; set; }
}

/// <summary>
/// Endpoint to start a new conversational chat session
/// </summary>
public class StartChat(
  IConversationService conversationService,
  ILogger<StartChat> logger)
  : Endpoint<StartChatRequest, StartChatResponse>
{
  public override void Configure()
  {
    Post("/api/chat/start");
    AllowAnonymous();
    Options(x => x.RequireCors());
    Summary(s =>
    {
      s.Summary = "Start a new conversational chat";
      s.Description = "Initiates a conversation with the AI assistant";
    });
  }

  public override async Task HandleAsync(StartChatRequest req, CancellationToken ct)
  {
    logger.LogInformation("Starting chat for user {UserId}", req.UserId);

    // Create conversation thread
    var conversationState = await conversationService.CreateThreadAsync(req.UserId, ct);
    var threadId = conversationState.ThreadId;

    await SendAsync(new StartChatResponse
    {
      ThreadId = threadId
    }, cancellation: ct);
  }
}
