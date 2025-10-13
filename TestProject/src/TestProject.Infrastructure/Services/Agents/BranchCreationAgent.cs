using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Conversation;

namespace TestProject.Infrastructure.Services.Agents;

public class BranchCreationAgent(
  IAzureDevOpsService devOpsService,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<BranchCreationAgent> logger)
  : ReflectingExecutor<BranchCreationAgent>("BranchCreationAgent"),
    IMessageHandler<BranchCreated, BranchCreated>
{
  public async ValueTask<BranchCreated> HandleAsync(
    BranchCreated branchData,
    IWorkflowContext context)
  {
    // Get thread ID from context provider
    var threadId = contextProvider.GetCurrentThreadId();

    // Send message that we're ready to create branch
    await SendMessageAsync(threadId, $"Ready to create branch: {branchData.BranchName}");

    // Request approval
    var approval = new ApprovalRequest
    {
      Id = Guid.NewGuid().ToString(),
      Question = "Should I create this branch?",
      Context = $"Branch name: {branchData.BranchName}\nBase: main",
      Step = WorkflowPhase.CreatingBranch,
      Data = new { branchName = branchData.BranchName }
    };

    await conversationService.RequestApprovalAsync(threadId, approval, CancellationToken.None);

    // Wait for approval
    var approved = await WaitForApprovalAsync(threadId, approval.Id);

    if (!approved)
    {
      await SendMessageAsync(threadId, "Branch creation cancelled by user");
      throw new OperationCanceledException("User rejected branch creation");
    }

    await SendMessageAsync(threadId, "Creating branch...");

    logger.LogInformation("Creating Azure DevOps branch for ETW detector");
    var branchName = branchData.BranchName;
    var repoUrl = await devOpsService.CreateBranchAsync(branchName, "main", CancellationToken.None);

    logger.LogInformation("Created branch {Branch} at {Url}", branchName, repoUrl);
    await SendMessageAsync(threadId, $"✓ Branch created: {branchName}");

    // Return updated branch info with repo URL
    return branchData with { RepositoryUrl = repoUrl };
  }

  private async Task<bool> WaitForApprovalAsync(Guid threadId, string approvalId)
  {
    // Poll for approval response (timeout after 30 minutes)
    var timeout = TimeSpan.FromMinutes(30);
    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < timeout)
    {
      var state = await conversationService.GetThreadStateAsync(threadId, CancellationToken.None);
      if (state == null) return false;

      // Check if approval is no longer pending
      var isPending = state.PendingApprovals.Any(a => a.Id == approvalId);
      if (!isPending)
      {
        // Check conversation history for approval response
        var approvalResponse = state.ConversationHistory
          .LastOrDefault(h => h.Contains(approvalId));
        return approvalResponse?.Contains("Approved") ?? false;
      }

      await Task.Delay(500);
    }

    logger.LogWarning("Approval {ApprovalId} timed out", approvalId);
    return false;
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
