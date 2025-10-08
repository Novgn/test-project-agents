using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Agents.Executors;

public class PRCreationExecutor(
  IAzureDevOpsService devOpsService,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<PRCreationExecutor> logger)
  : ReflectingExecutor<PRCreationExecutor>("PRCreationExecutor"),
    IMessageHandler<ChatMessage, PRCreated>
{
  public async ValueTask<PRCreated> HandleAsync(
    ChatMessage codeGenMessage,
    IWorkflowContext context)
  {
    // Get thread ID from context provider
    var threadId = contextProvider.GetCurrentThreadId();

    logger.LogInformation("Creating pull request for generated detector code");

    // Extract generated code from agent's response
    var detectorCode = codeGenMessage.Text ?? throw new InvalidOperationException("No code generated");

    // Send message with generated code summary
    await SendMessageAsync(threadId, "Code generation complete! Ready to create pull request.");

    // Request approval for PR creation
    var branchName = $"feature/etw-detector-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var title = $"Add ETW Detector - {DateTime.UtcNow:yyyy-MM-dd}";

    var approval = new ApprovalRequest
    {
      Id = Guid.NewGuid().ToString(),
      Question = "Should I create a pull request with the generated code?",
      Context = $"PR Title: {title}\nBranch: {branchName}\n\nThe PR will include the generated detector code and tests.",
      Step = WorkflowPhase.CreatingPullRequest,
      Data = new { branchName, title, codePreview = detectorCode.Substring(0, Math.Min(200, detectorCode.Length)) + "..." }
    };

    await conversationService.RequestApprovalAsync(threadId, approval, CancellationToken.None);

    // Wait for approval
    var approved = await WaitForApprovalAsync(threadId, approval.Id);

    if (!approved)
    {
      await SendMessageAsync(threadId, "Pull request creation cancelled by user");
      throw new OperationCanceledException("User rejected PR creation");
    }

    await SendMessageAsync(threadId, "Creating pull request...");

    var description = $@"
## Generated ETW Detector

This PR adds a new ETW detector.

**Generated Code:**
```csharp
{detectorCode}
```

## Review Required
Please review the generated detector code and approve for deployment.
";

    var prResult = await devOpsService.CreatePullRequestAsync(
      branchName,
      "main",
      title,
      description,
      CancellationToken.None);

    await SendMessageAsync(threadId, $"✓ Pull request created: PR #{prResult.Id}");

    var generatedCode = new GeneratedCode(
      detectorCode,
      "Detectors/GeneratedDetector.cs",
      new PRPatterns("Standard patterns", "Code conventions", null!));

    return new PRCreated(
      prResult.Id,
      prResult.Url,
      generatedCode,
      RequiresApproval: true);
  }

  private async Task<bool> WaitForApprovalAsync(Guid threadId, string approvalId)
  {
    var timeout = TimeSpan.FromMinutes(30);
    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < timeout)
    {
      var state = await conversationService.GetThreadStateAsync(threadId, CancellationToken.None);
      if (state == null) return false;

      var isPending = state.PendingApprovals.Any(a => a.Id == approvalId);
      if (!isPending)
      {
        var approvalResponse = state.ConversationHistory.LastOrDefault(h => h.Contains(approvalId));
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
