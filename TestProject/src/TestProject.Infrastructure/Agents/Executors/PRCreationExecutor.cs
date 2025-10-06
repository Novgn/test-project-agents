using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Agents;

namespace TestProject.Infrastructure.Agents.Executors;

public class PRCreationExecutor(
  IAzureDevOpsService devOpsService,
  ILogger<PRCreationExecutor> logger)
  : ReflectingExecutor<PRCreationExecutor>("PRCreationExecutor"),
    IMessageHandler<ChatMessage, PRCreated>
{
  public async ValueTask<PRCreated> HandleAsync(
    ChatMessage codeGenMessage,
    IWorkflowContext context)
  {
    logger.LogInformation("Creating pull request for generated detector code");

    // Extract generated code from agent's response
    var detectorCode = codeGenMessage.Text ?? throw new InvalidOperationException("No code generated");

    // For this simplified version, we'll create a PR directly
    var branchName = $"feature/etw-detector-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var title = $"Add ETW Detector - {DateTime.UtcNow:yyyy-MM-dd}";
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
}
