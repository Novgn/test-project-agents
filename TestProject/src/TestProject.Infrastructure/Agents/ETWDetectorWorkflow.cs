using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TestProject.Infrastructure.Agents.Executors;

namespace TestProject.Infrastructure.Agents;

public class ETWDetectorWorkflow(
  IConfiguration configuration,
  IServiceProvider serviceProvider,
  ILogger<ETWDetectorWorkflow> logger)
{
  public Workflow BuildWorkflow()
  {
    logger.LogInformation("Building ETW Detector Workflow using Microsoft Agent Framework");

    // Create AI Chat Client for agents
    var endpoint = configuration["AzureOpenAI:Endpoint"]
      ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
    var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

    var chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
      .GetChatClient(deploymentName)
      .AsIChatClient();

    // Create AI Agents for intelligence steps
    var validationAgent = new ChatClientAgent(
      chatClient,
      name: "ETWValidator",
      instructions: @"You are an ETW (Event Tracing for Windows) expert.
        Validate the provided ETW provider details and ensure they are properly formatted.
        Echo back the ETW details if valid, or explain what is wrong if invalid.
        Be concise and direct.");

    var prAnalysisAgent = new ChatClientAgent(
      chatClient,
      name: "PRAnalyzer",
      instructions: @"You are a code review expert specializing in ETW detectors.
        Given the branch and Kusto information, provide guidance on code patterns and conventions.
        Keep your response focused and actionable.");

    var codeGenAgent = new ChatClientAgent(
      chatClient,
      name: "CodeGenerator",
      instructions: @"You are an expert C# developer specializing in ETW detector implementation.
        Generate production-quality detector code based on the provided information.
        Provide complete, compilable code following best practices.");

    // Get custom executors from DI
    var kustoExecutor = serviceProvider.GetRequiredService<KustoQueryExecutor>();
    var branchExecutor = serviceProvider.GetRequiredService<BranchCreationExecutor>();
    var prExecutor = serviceProvider.GetRequiredService<PRCreationExecutor>();
    var deploymentExecutor = serviceProvider.GetRequiredService<DeploymentMonitorExecutor>();

    // Build the sequential workflow using Microsoft Agent Framework patterns
    var workflow = new WorkflowBuilder(validationAgent)
      // Step 1: Validation Agent validates ETW input
      .AddEdge(validationAgent, kustoExecutor)

      // Step 2: Kusto Executor queries Azure Data Explorer
      .AddEdge(kustoExecutor, branchExecutor)

      // Step 3: Branch Executor creates Azure DevOps branch
      .AddEdge(branchExecutor, prAnalysisAgent)

      // Step 4: PR Analysis Agent analyzes patterns
      .AddEdge(prAnalysisAgent, codeGenAgent)

      // Step 5: Code Generation Agent generates code
      .AddEdge(codeGenAgent, prExecutor)

      // Step 6: PR Executor creates pull request
      .AddEdge(prExecutor, deploymentExecutor)

      // Step 7: Deployment Monitor provides final status
      .WithOutputFrom(deploymentExecutor)
      .Build();

    logger.LogInformation("ETW Detector Workflow built successfully with 7 steps");

    return workflow;
  }
}

