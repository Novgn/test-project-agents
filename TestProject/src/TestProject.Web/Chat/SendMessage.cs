using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Agents;

namespace TestProject.Web.Chat;

public class SendChatMessageRequest
{
  public Guid ThreadId { get; set; }
  public string Message { get; set; } = string.Empty;
}

public class SendChatMessageResponse
{
  public Guid ThreadId { get; set; }
  public string Response { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint for sending chat messages in a conversational loop
/// </summary>
public class SendMessage(
  IConversationService conversationService,
  ConversationalChatService chatService,
  IWorkflowOrchestrationService workflowService,
  ILogger<SendMessage> logger)
  : Endpoint<SendChatMessageRequest, SendChatMessageResponse>
{
  public override void Configure()
  {
    Post("/api/chat/send");
    AllowAnonymous();
    Options(x => x.RequireCors());
    Summary(s =>
    {
      s.Summary = "Send a message in a conversational chat";
      s.Description = "Sends a user message and gets an AI response";
    });
  }

  public override async Task HandleAsync(SendChatMessageRequest req, CancellationToken ct)
  {
    logger.LogInformation("Received chat message for thread {ThreadId}: {Message}",
      req.ThreadId, req.Message);

    // Add user message to conversation
    var userMessage = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = ConversationMessageType.UserResponse,
      Content = req.Message
    };

    await conversationService.AddMessageAsync(req.ThreadId, userMessage, ct);

    // Get AI response using Azure OpenAI
    var aiResponse = await chatService.ProcessUserMessageAsync(req.ThreadId, req.Message, ct);

    // Check if AI determined we have enough information to start the workflow
    var shouldStartWorkflow = aiResponse.Contains("READY_TO_START_WORKFLOW", StringComparison.OrdinalIgnoreCase);

    // Remove the workflow marker before sending to user
    aiResponse = aiResponse.Replace("READY_TO_START_WORKFLOW", "").Trim();

    var responseMessage = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = ConversationMessageType.AgentMessage,
      Content = aiResponse
    };

    await conversationService.AddMessageAsync(req.ThreadId, responseMessage, ct);

    // If ready, start the agent workflow execution
    if (shouldStartWorkflow)
    {
      logger.LogInformation("Starting workflow execution for thread {ThreadId}", req.ThreadId);

      // Get conversation state to extract ETW details
      var state = await conversationService.GetThreadStateAsync(req.ThreadId, ct);
      var etwDetails = ExtractETWDetailsFromConversation(state);

      // Start the Microsoft Agent Framework workflow
      _ = Task.Run(async () =>
      {
        try
        {
          var workflowId = await workflowService.StartWorkflowAsync(
            state!.UserId,
            etwDetails,
            req.ThreadId,
            CancellationToken.None);

          logger.LogInformation("Workflow {WorkflowId} started for thread {ThreadId}",
            workflowId, req.ThreadId);
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "Failed to start workflow for thread {ThreadId}", req.ThreadId);
        }
      }, CancellationToken.None);
    }

    await SendAsync(new SendChatMessageResponse
    {
      ThreadId = req.ThreadId,
      Response = aiResponse
    }, cancellation: ct);
  }

  private string ExtractETWDetailsFromConversation(ConversationState? state)
  {
    if (state == null || state.ConversationHistory.Count == 0)
      return "ETW Provider Details";

    // Extract the first user message as ETW details
    var firstUserMessage = state.ConversationHistory
      .FirstOrDefault(msg => msg.StartsWith("User: "));

    return firstUserMessage?.Substring(6) ?? "ETW Provider Details";
  }
}
