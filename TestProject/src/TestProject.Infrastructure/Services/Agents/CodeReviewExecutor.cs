using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Conversation;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace TestProject.Infrastructure.Services.Agents;

/// <summary>
/// Executor that wraps the AI-powered code review agent
/// Reviews generated code for quality, best practices, and potential issues
/// </summary>
public class CodeReviewExecutor(
  AIAgent codeReviewAgent,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<CodeReviewExecutor> logger)
  : ReflectingExecutor<CodeReviewExecutor>("CodeReviewExecutor"),
    IMessageHandler<ChatMessage, ChatMessage>
{
  public async ValueTask<ChatMessage> HandleAsync(
    ChatMessage generatedCode,
    IWorkflowContext context)
  {
    var threadId = contextProvider.GetCurrentThreadId();
    logger.LogInformation("Reviewing generated code with AI...");

    await SendMessageAsync(threadId, "\n\n🔍 **Code Review in Progress...**\n");

    // Build the prompt for the AI agent
    var userPrompt = $@"Review the following C# detector code and provide feedback:

```csharp
{generatedCode.Text}
```

Please provide:
1. Overall code quality assessment
2. Potential issues or bugs
3. Best practice violations
4. Documentation improvements
5. Specific recommendations for improvement

Format your response as markdown with clear sections.";

    // Get or create thread for the AI agent
    var agentThread = codeReviewAgent.GetNewThread();

    logger.LogInformation("Starting streaming code review...");

    // Call the AI agent with streaming - tokens stream in real-time to UI
    var reviewBuilder = new System.Text.StringBuilder();

    await foreach (var update in codeReviewAgent.RunStreamingAsync(userPrompt, agentThread))
    {
      var chunk = update.Text;
      if (!string.IsNullOrEmpty(chunk))
      {
        reviewBuilder.Append(chunk);

        // Stream each chunk to the UI in real-time via SignalR
        await SendMessageAsync(threadId, chunk);
      }
    }

    var reviewFeedback = reviewBuilder.ToString();

    if (string.IsNullOrWhiteSpace(reviewFeedback))
    {
      throw new InvalidOperationException("Failed to generate code review - empty response");
    }

    logger.LogInformation("✓ Code review complete: {Length} characters", reviewFeedback.Length);
    await SendMessageAsync(threadId, $"\n\n✓ Code review complete\n");

    // Return the original generated code (not the review) to pass to next step
    // The review is just displayed to the user for visibility
    return generatedCode;
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
