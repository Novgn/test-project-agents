using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Conversation;

namespace TestProject.Infrastructure.Services.Agents;

/// <summary>
/// AI agent that generates detector code based on Kusto query results and ETW schema
/// Uses LLM to create conversational, context-aware code generation
/// </summary>
public class CodeGenerationAgent(
  IChatClient chatClient,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<CodeGenerationAgent> logger)
  : ReflectingExecutor<CodeGenerationAgent>("CodeGenerationAgent"),
    IMessageHandler<BranchCreated, ChatMessage>
{
  public async ValueTask<ChatMessage> HandleAsync(
    BranchCreated branchData,
    IWorkflowContext context)
  {
    var threadId = contextProvider.GetCurrentThreadId();
    logger.LogInformation("Generating detector code for branch {Branch}", branchData.BranchName);

    await SendMessageAsync(threadId, "🤖 Generating detector code using AI...");

    // Build the prompt for code generation using Kusto results and ETW details
    var systemPrompt = @"You are an expert C# developer specializing in ETW (Event Tracing for Windows) detectors.
Generate production-ready detector code based on the ETW schema and existing patterns from the Kusto database.

Your code should:
- Follow best practices and patterns from existing detectors
- Include proper error handling and logging
- Be well-documented with XML comments
- Include unit test suggestions
- Use the provided ETW schema properties for event parsing";

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

    // Use the conversational AI to generate code
    var messages = new List<ChatMessage>
    {
      new(ChatRole.System, systemPrompt),
      new(ChatRole.User, userPrompt)
    };

    await SendMessageAsync(threadId, "Analyzing ETW schema and existing patterns...");

    var response = await chatClient.GetResponseAsync(
      messages,
      new ChatOptions
      {
        Temperature = 0.3f, // Lower temperature for more deterministic code generation
        MaxOutputTokens = 4000
      });

    var generatedCode = response?.Text ?? throw new InvalidOperationException("Failed to generate code");

    logger.LogInformation("Generated detector code: {Length} characters", generatedCode.Length);
    await SendMessageAsync(threadId, $"✓ Generated detector code ({generatedCode.Length} characters)");

    // Preview the code to the user
    var codePreview = generatedCode.Length > 500
      ? string.Concat(generatedCode.AsSpan(0, 500), "...")
      : generatedCode;

    await SendMessageAsync(threadId, $"**Code Preview:**\n```csharp\n{codePreview}\n```");

    // Return the generated code as a ChatMessage for the next executor
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
