using Microsoft.Agents.AI.Workflows;
using TestProject.Infrastructure.Agents.Executors;

namespace TestProject.Infrastructure.Agents;

public class ETWDetectorWorkflow(
  IServiceProvider serviceProvider,
  ILogger<ETWDetectorWorkflow> logger)
{
  public Workflow BuildWorkflow()
  {
    logger.LogInformation("Building ETW Detector Workflow using Microsoft Agent Framework");

    // Get executors from DI
    var aiAgent = serviceProvider.GetRequiredService<AIValidationAgent>();
    var kustoExecutor = serviceProvider.GetRequiredService<KustoQueryExecutor>();

    // Build conversational workflow chain: AI Agent → Kusto Query
    // The workflow automatically passes typed outputs to the next executor
    // AIValidationAgent: ETWInput → ChatMessage
    // KustoQueryExecutor: ChatMessage → BranchCreated
    var builder = new WorkflowBuilder(aiAgent);
    builder.AddEdge(aiAgent, kustoExecutor);

    var workflow = builder
      .WithOutputFrom(kustoExecutor)
      .Build();

    logger.LogInformation("ETW Detector Workflow built successfully: AI Validation → Kusto Query");

    return workflow;
  }
}

