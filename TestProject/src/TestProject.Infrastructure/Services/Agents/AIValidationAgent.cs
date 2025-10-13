using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Conversation;

namespace TestProject.Infrastructure.Services.Agents;

/// <summary>
/// AI agent that validates ETW provider details and initiates the workflow execution
/// </summary>
public class AIValidationAgent(
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<AIValidationAgent> logger)
  : ReflectingExecutor<AIValidationAgent>("AIValidationAgent"),
    IMessageHandler<ETWInput, ChatMessage>
{
  public async ValueTask<ChatMessage> HandleAsync(
    ETWInput input,
    IWorkflowContext context)
  {
    var threadId = contextProvider.GetCurrentThreadId();
    logger.LogInformation("AI Validation Agent processing ETW details for user {UserId}", input.UserId);

    // Notify that workflow execution has started
    await SendMessageAsync(threadId, $"✓ Validated ETW provider: **{input.ETWDetails}**");

    // Return ETW details as ChatMessage for next executor
    return new ChatMessage(ChatRole.Assistant, input.ETWDetails);
  }

  private async Task SendMessageAsync(Guid threadId, string content)
  {
    var message = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = ConversationMessageType.AgentMessage,
      Content = content
    };
    await conversationService.AddMessageAsync(threadId, message, CancellationToken.None);
  }
}
