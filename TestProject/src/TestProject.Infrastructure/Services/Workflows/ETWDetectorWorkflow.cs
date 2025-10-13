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

    // Get agents from DI
    var aiAgent = serviceProvider.GetRequiredService<AIValidationAgent>();
    var kustoAgent = serviceProvider.GetRequiredService<KustoQueryAgent>();
    var branchAgent = serviceProvider.GetRequiredService<BranchCreationAgent>();
    var codeGenAgent = serviceProvider.GetRequiredService<CodeGenerationAgent>();
    var prAgent = serviceProvider.GetRequiredService<PRCreationAgent>();
    var deploymentAgent = serviceProvider.GetRequiredService<DeploymentMonitorAgent>();

    // Build complete conversational workflow chain
    // The framework automatically routes messages based on input/output types:
    //
    // 1. AIValidationAgent: ETWInput → ChatMessage
    // 2. KustoQueryAgent: ChatMessage → BranchCreated
    // 3. BranchCreationAgent: BranchCreated → BranchCreated (creates branch with approval)
    // 4. CodeGenerationAgent: BranchCreated → ChatMessage (generates detector code using AI)
    // 5. PRCreationAgent: ChatMessage → PRCreated (creates PR with approval)
    // 6. DeploymentMonitorAgent: PRCreated → string (monitors deployment)

    var builder = new WorkflowBuilder(aiAgent);

    // Build the complete workflow chain
    builder.AddEdge(aiAgent, kustoAgent);
    builder.AddEdge(kustoAgent, branchAgent);
    builder.AddEdge(branchAgent, codeGenAgent);
    builder.AddEdge(codeGenAgent, prAgent);
    builder.AddEdge(prAgent, deploymentAgent);

    var workflow = builder
      .WithOutputFrom(deploymentAgent)
      .Build();

    logger.LogInformation("ETW Detector Workflow built successfully with complete chain: Validation → Kusto → Branch → CodeGen → PR → Deployment");

    return workflow;
  }
}

