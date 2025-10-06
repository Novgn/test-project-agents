using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Agents;

namespace TestProject.Infrastructure.Agents.Executors;

public class KustoQueryExecutor(
  IKustoQueryService kustoService,
  ILogger<KustoQueryExecutor> logger)
  : ReflectingExecutor<KustoQueryExecutor>("KustoQueryExecutor"),
    IMessageHandler<ChatMessage, BranchCreated>
{
  public async ValueTask<BranchCreated> HandleAsync(
    ChatMessage validationMessage,
    IWorkflowContext context)
  {
    // Extract ETW details from the validation agent's response
    var etwDetails = validationMessage.Text ?? throw new InvalidOperationException("No ETW details provided");

    logger.LogInformation("Querying Kusto for ETW provider: {Details}", etwDetails);

    var result = await kustoService.FindConvertersAndDetectorsAsync(
      etwDetails,
      CancellationToken.None);

    // For now, create a simple branch name - this will be enhanced
    var branchName = $"feature/etw-detector-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var input = new ETWInput("system", etwDetails);

    return new BranchCreated(branchName, "", result, input);
  }
}
