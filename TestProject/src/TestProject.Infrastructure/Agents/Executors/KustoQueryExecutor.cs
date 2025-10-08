using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Agents.Executors;

public class KustoQueryExecutor(
  IKustoQueryService kustoService,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<KustoQueryExecutor> logger)
  : ReflectingExecutor<KustoQueryExecutor>("KustoQueryExecutor"),
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
    await SendMessageAsync(threadId, "Querying Azure Kusto cluster for existing converters and detectors...");

    logger.LogInformation("Querying Kusto for ETW provider: {Details}", etwDetails);

    var result = await kustoService.FindConvertersAndDetectorsAsync(
      etwDetails,
      CancellationToken.None);

    await SendMessageAsync(threadId, $"✓ Found {result.Converters.Length} converters and {result.ExistingDetectors.Length} existing detectors in the system");

    // For now, create a simple branch name - this will be enhanced
    var branchName = $"feature/etw-detector-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var input = new ETWInput("system", etwDetails);

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
