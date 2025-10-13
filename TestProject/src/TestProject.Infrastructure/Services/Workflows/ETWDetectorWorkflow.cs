using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using TestProject.Infrastructure.Services.Agents;

namespace TestProject.Infrastructure.Services.Workflows;

public class ETWDetectorWorkflow(
  IServiceProvider serviceProvider,
  ILogger<ETWDetectorWorkflow> logger)
{
  public Workflow BuildWorkflow()
  {
    logger.LogInformation("Building ETW Detector Workflow using Microsoft Agent Framework");

    // Get executor agents from DI (for business logic)
    var aiAgent = serviceProvider.GetRequiredService<AIValidationAgent>();
    var kustoAgent = serviceProvider.GetRequiredService<KustoQueryAgent>();
    var branchAgent = serviceProvider.GetRequiredService<BranchCreationAgent>();
    var codeGenExecutor = serviceProvider.GetRequiredService<CodeGenerationExecutor>();
    var codeReviewExecutor = serviceProvider.GetRequiredService<CodeReviewExecutor>();
    var prAgent = serviceProvider.GetRequiredService<PRCreationAgent>();
    var deploymentAgent = serviceProvider.GetRequiredService<DeploymentMonitorAgent>();

    // Build complete conversational workflow chain with multi-agent orchestration
    // The framework automatically routes messages based on input/output types:
    //
    // 1. AIValidationAgent: ETWInput → ChatMessage
    // 2. KustoQueryAgent: ChatMessage → BranchCreated
    // 3. BranchCreationAgent: BranchCreated → BranchCreated (creates branch with approval)
    // 4. CodeGenerationExecutor: BranchCreated → ChatMessage (AI agent generates code with function calling)
    // 5. CodeReviewExecutor: ChatMessage → ChatMessage (AI agent reviews generated code)
    // 6. PRCreationAgent: ChatMessage → PRCreated (creates PR with approval)
    // 7. DeploymentMonitorAgent: PRCreated → string (monitors deployment)

    var builder = new WorkflowBuilder(aiAgent);

    // Build the complete workflow chain with sequential code gen → review orchestration
    builder.AddEdge(aiAgent, kustoAgent);
    builder.AddEdge(kustoAgent, branchAgent);
    builder.AddEdge(branchAgent, codeGenExecutor);  // AI agent with Kusto function calling
    builder.AddEdge(codeGenExecutor, codeReviewExecutor);  // Sequential: code gen → review
    builder.AddEdge(codeReviewExecutor, prAgent);
    builder.AddEdge(prAgent, deploymentAgent);

    var workflow = builder
      .WithOutputFrom(deploymentAgent)
      .Build();

    logger.LogInformation("ETW Detector Workflow built successfully with multi-agent orchestration: Validation → Kusto → Branch → CodeGen (AI) → Review (AI) → PR → Deployment");

    return workflow;
  }
}

