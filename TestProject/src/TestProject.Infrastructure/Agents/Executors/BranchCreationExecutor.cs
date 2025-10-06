using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Agents;

namespace TestProject.Infrastructure.Agents.Executors;

public class BranchCreationExecutor(
  IAzureDevOpsService devOpsService,
  ILogger<BranchCreationExecutor> logger)
  : ReflectingExecutor<BranchCreationExecutor>("BranchCreationExecutor"),
    IMessageHandler<BranchCreated, BranchCreated>
{
  public async ValueTask<BranchCreated> HandleAsync(
    BranchCreated branchData,
    IWorkflowContext context)
  {
    logger.LogInformation("Creating Azure DevOps branch for ETW detector");

    var branchName = branchData.BranchName;
    var repoUrl = await devOpsService.CreateBranchAsync(branchName, "main", CancellationToken.None);

    logger.LogInformation("Created branch {Branch} at {Url}", branchName, repoUrl);

    // Return updated branch info with repo URL
    return branchData with { RepositoryUrl = repoUrl };
  }
}
