using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Agents;
using TestProject.Infrastructure.Services.Agents.Tools;
using TestProject.Infrastructure.Services.Azure;
using TestProject.Infrastructure.Services.Workflows;
using TestProject.Infrastructure.Services.Conversation;
using TestProject.Infrastructure.Data;

namespace TestProject.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);
    services.AddDbContext<AppDbContext>(options =>
     options.UseSqlite(connectionString));

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

    // Azure Services
    services.AddSingleton<IKustoQueryService, KustoQueryService>();
    services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();

    // AI Function Tools
    services.AddSingleton<KustoQueryTool>();

    // Azure OpenAI Chat Client for Microsoft Agent Framework
    services.AddSingleton<IChatClient>(sp =>
    {
      var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
      var deploymentName = config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("Azure OpenAI deployment name not configured");
      var apiKey = config["AzureOpenAI:ApiKey"];

      AzureOpenAIClient azureClient;
      if (!string.IsNullOrEmpty(apiKey))
      {
        // Use API Key authentication (development)
        logger.LogInformation("Using API Key authentication for Azure OpenAI");
        azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
      }
      else
      {
        // Use DefaultAzureCredential (production - requires RBAC permissions)
        logger.LogInformation("Using DefaultAzureCredential for Azure OpenAI");
        azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
      }

      return azureClient.GetChatClient(deploymentName).AsIChatClient();
    });

    // AI Agent for Code Generation (uses LLM reasoning with Kusto tool)
    services.AddKeyedSingleton<AIAgent>("CodeGenerationAgent", (sp, key) =>
    {
      var chatClient = sp.GetRequiredService<IChatClient>();
      var kustoTool = sp.GetRequiredService<KustoQueryTool>();

      return new ChatClientAgent(
        chatClient,
        name: "CodeGenerationAgent",
        instructions: @"You are an expert C# developer specializing in ETW (Event Tracing for Windows) detectors.
Generate production-ready detector code based on the ETW schema and existing patterns from the Kusto database.

Your code should:
- Follow best practices and patterns from existing detectors
- Include proper error handling and logging
- Be well-documented with XML comments
- Include unit test suggestions
- Use the provided ETW schema properties for event parsing

When you receive information about an ETW provider, you can use the QueryDetectorPatterns function to look up existing detector patterns, converters, and best practices from the Kusto database before generating code.
Generate complete C# detector class code based on all available information.",
        tools: [AIFunctionFactory.Create(kustoTool.QueryDetectorPatterns)]);
    });

    // AI Agent for Code Review (uses LLM reasoning)
    services.AddKeyedSingleton<AIAgent>("CodeReviewAgent", (sp, key) =>
    {
      var chatClient = sp.GetRequiredService<IChatClient>();

      return new ChatClientAgent(
        chatClient,
        name: "CodeReviewAgent",
        instructions: @"You are an expert C# code reviewer specializing in ETW detectors and production-grade code quality.

Review code for:
- **Code Quality**: Readability, maintainability, and adherence to C# conventions
- **Best Practices**: SOLID principles, design patterns, and ETW-specific patterns
- **Error Handling**: Exception handling, edge cases, and defensive programming
- **Documentation**: XML comments, inline comments, and clarity
- **Testing**: Suggest unit tests, integration tests, and edge cases to cover
- **Performance**: Identify potential bottlenecks or inefficiencies
- **Security**: Check for potential security issues or vulnerabilities

Provide constructive, actionable feedback with specific line references and improvement suggestions.");
    });

    // Infrastructure Services (Workflow Management)
    services.AddSingleton<WorkflowContextProvider>();
    services.AddSingleton<IWorkflowOrchestrationService, WorkflowOrchestrationService>();
    services.AddSingleton<ETWDetectorWorkflow>();

    // Workflow Executors (business logic components)
    services.AddTransient<AIValidationAgent>();
    services.AddTransient<KustoQueryAgent>();
    services.AddTransient<BranchCreationAgent>();
    services.AddTransient<CodeGenerationExecutor>(sp =>
      new CodeGenerationExecutor(
        sp.GetRequiredKeyedService<AIAgent>("CodeGenerationAgent"),
        sp.GetRequiredService<IConversationService>(),
        sp.GetRequiredService<WorkflowContextProvider>(),
        sp.GetRequiredService<ILogger<CodeGenerationExecutor>>()));
    services.AddTransient<CodeReviewExecutor>(sp =>
      new CodeReviewExecutor(
        sp.GetRequiredKeyedService<AIAgent>("CodeReviewAgent"),
        sp.GetRequiredService<IConversationService>(),
        sp.GetRequiredService<WorkflowContextProvider>(),
        sp.GetRequiredService<ILogger<CodeReviewExecutor>>()));
    services.AddTransient<PRCreationAgent>();
    services.AddTransient<DeploymentMonitorAgent>();

    logger.LogInformation("{Project} services registered with Microsoft Agent Framework", "Infrastructure");

    return services;
  }
}
