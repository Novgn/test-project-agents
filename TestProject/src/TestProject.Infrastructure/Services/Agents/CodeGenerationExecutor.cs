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
/// Executor that wraps the AI-powered code generation agent
/// Formats BranchCreated data into prompts for the AI agent
/// </summary>
public class CodeGenerationExecutor(
  AIAgent codeGenAgent,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<CodeGenerationExecutor> logger)
  : ReflectingExecutor<CodeGenerationExecutor>("CodeGenerationExecutor"),
    IMessageHandler<BranchCreated, ChatMessage>
{
  public async ValueTask<ChatMessage> HandleAsync(
    BranchCreated branchData,
    IWorkflowContext context)
  {
    var threadId = contextProvider.GetCurrentThreadId();
    logger.LogInformation("Generating detector code for branch {Branch}", branchData.BranchName);

    await SendMessageAsync(threadId, "🤖 Generating detector code using AI...");

    // Build the prompt for the AI agent with all context
    var userPrompt = $@"Generate an ETW detector with the following details:

**Provider Information:**
- Provider ID: {branchData.Input.ProviderId}
- Rule ID: {branchData.Input.RuleId}

**ETW Schema (JSON):**
```json
{branchData.Input.ETWSchemaJson}
```

**Existing Converters Found:** {string.Join(", ", branchData.KustoData.Converters)}
**Existing Detectors Found:** {string.Join(", ", branchData.KustoData.ExistingDetectors)}

**Recommended Configuration:**
{string.Join("\n", branchData.KustoData.RecommendedConfiguration.Select(kvp => $"- {kvp.Key}: {kvp.Value}"))}

Please generate the complete C# detector class code.";

    // Get or create thread for the AI agent (maintains conversation history)
    var agentThread = codeGenAgent.GetNewThread();

    logger.LogInformation("Starting streaming code generation...");
    await SendMessageAsync(threadId, "```csharp\n");

    // Call the AI agent with streaming - tokens stream in real-time to UI
    var generatedCodeBuilder = new System.Text.StringBuilder();

    await foreach (var update in codeGenAgent.RunStreamingAsync(userPrompt, agentThread))
    {
      var chunk = update.Text;
      if (!string.IsNullOrEmpty(chunk))
      {
        generatedCodeBuilder.Append(chunk);

        // Stream each chunk to the UI in real-time via SignalR
        await SendMessageAsync(threadId, chunk);
      }
    }

    await SendMessageAsync(threadId, "\n```");

    var generatedCode = generatedCodeBuilder.ToString();

    if (string.IsNullOrWhiteSpace(generatedCode))
    {
      throw new InvalidOperationException("Failed to generate code - empty response");
    }

    logger.LogInformation("✓ Generated detector code: {Length} characters", generatedCode.Length);
    await SendMessageAsync(threadId, $"\n✓ Code generation complete ({generatedCode.Length} characters)");

    // Return as ChatMessage for next executor (PRCreationAgent)
    return new ChatMessage(ChatRole.Assistant, generatedCode);
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
