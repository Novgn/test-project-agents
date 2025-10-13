using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Conversation;

namespace TestProject.Infrastructure.Services.Agents;

public class KustoQueryAgent(
  IKustoQueryService kustoService,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<KustoQueryAgent> logger)
  : ReflectingExecutor<KustoQueryAgent>("KustoQueryAgent"),
    IMessageHandler<ChatMessage, BranchCreated>
{
  public async ValueTask<BranchCreated> HandleAsync(
    ChatMessage validationMessage,
    IWorkflowContext context)
  {
    // Get thread ID from context provider
    var threadId = contextProvider.GetCurrentThreadId();

    // Extract ETW details from the validation agent's response
    var etwDetails = validationMessage.Text ?? throw new InvalidOperationException("No ETW details provided");

    await SendMessageAsync(threadId, $"ETW details validated: {etwDetails}");

    // Parse ETW details to extract providerId, ruleId, and schema
    // Expected format: JSON with ProviderId, RuleId, and Schema properties
    string providerId;
    string ruleId;
    string schemaJson;

    try
    {
      using var doc = JsonDocument.Parse(etwDetails);
      var root = doc.RootElement;

      providerId = root.GetProperty("ProviderId").GetString()
        ?? throw new InvalidOperationException("ProviderId not found in ETW details");
      ruleId = root.GetProperty("RuleId").GetString()
        ?? throw new InvalidOperationException("RuleId not found in ETW details");

      // Schema properties are already in JSON format
      if (root.TryGetProperty("Schema", out var schemaElement))
      {
        schemaJson = schemaElement.GetRawText();
      }
      else
      {
        schemaJson = "{}";
      }
    }
    catch (JsonException ex)
    {
      logger.LogError(ex, "Failed to parse ETW details as JSON: {Details}", etwDetails);
      throw new InvalidOperationException("ETW details must be valid JSON with ProviderId and RuleId", ex);
    }

    await SendMessageAsync(threadId, "Querying Azure Kusto cluster for existing converters and detectors...");

    logger.LogInformation("Querying Kusto for ProviderId: {ProviderId}, RuleId: {RuleId}", providerId, ruleId);

    var result = await kustoService.FindConvertersAndDetectorsAsync(
      providerId,
      ruleId,
      CancellationToken.None);

    await SendMessageAsync(threadId, $"✓ Found {result.Converters.Length} converters and {result.ExistingDetectors.Length} existing detectors in the system");

    // For now, create a simple branch name - this will be enhanced
    var branchName = $"feature/etw-detector-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var input = new ETWInput("system", providerId, ruleId, schemaJson, etwDetails);

    return new BranchCreated(branchName, "", result, input);
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
