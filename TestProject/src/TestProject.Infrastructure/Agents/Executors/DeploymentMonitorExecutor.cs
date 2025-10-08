using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Agents.Executors;

public class DeploymentMonitorExecutor(
  IAzureDevOpsService devOpsService,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<DeploymentMonitorExecutor> logger)
  : ReflectingExecutor<DeploymentMonitorExecutor>("DeploymentMonitorExecutor"),
    IMessageHandler<PRCreated, string>
{
  public async ValueTask<string> HandleAsync(
    PRCreated prCreated,
    IWorkflowContext context)
  {
    // Get thread ID from context provider
    var threadId = contextProvider.GetCurrentThreadId();

    logger.LogInformation("Monitoring deployment status for PR {PrId}", prCreated.PullRequestId);

    await SendMessageAsync(threadId, "Monitoring deployment pipeline...");

    // Check deployment status
    var deploymentStatus = await devOpsService.GetDeploymentStatusAsync(
      prCreated.PullRequestId,
      CancellationToken.None);

    await SendMessageAsync(threadId, $"✓ Deployment status: {deploymentStatus.Status}");

    var result = $@"
ETW Detector Workflow Complete!

**Pull Request Created:**
- PR ID: {prCreated.PullRequestId}
- URL: {prCreated.PRUrl}
- Status: {deploymentStatus.Status}

**Deployment Status:**
- Build ID: {deploymentStatus.BuildId}
- Deployed At: {deploymentStatus.DeployedAt?.ToString() ?? "Pending"}
- Environments: {string.Join(", ", deploymentStatus.Environments)}

**Next Steps:**
1. Review the pull request at {prCreated.PRUrl}
2. Once approved, the detector will be deployed
3. Monitor performance metrics after deployment
";

    await SendMessageAsync(threadId, "🎉 Workflow completed successfully!");

    return result;
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
