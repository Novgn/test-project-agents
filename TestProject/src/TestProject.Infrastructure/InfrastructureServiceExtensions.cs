using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Services.Agents;
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

    // Azure OpenAI Chat Client for Microsoft Agent Framework
    services.AddSingleton(sp =>
    {
      var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
      var deploymentName = config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("Azure OpenAI deployment name not configured");
      var apiKey = config["AzureOpenAI:ApiKey"];

      AzureOpenAIClient azureClient;
      if (!string.IsNullOrEmpty(apiKey))
      {
        // Use API Key authentication (development)
        logger.LogInformation("Using API Key authentication for Azure OpenAI");
        azureClient = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));
      }
      else
      {
        // Use DefaultAzureCredential (production - requires RBAC permissions)
        logger.LogInformation("Using DefaultAzureCredential for Azure OpenAI");
        azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
      }

      return azureClient.GetChatClient(deploymentName).AsIChatClient();
    });

    // Infrastructure Services (Workflow Management)
    services.AddSingleton<WorkflowContextProvider>();
    services.AddSingleton<IWorkflowOrchestrationService, WorkflowOrchestrationService>();
    services.AddSingleton<ETWDetectorWorkflow>();

    // Workflow Agents (Microsoft Agent Framework - must be registered for DI)
    services.AddTransient<AIValidationAgent>();
    services.AddTransient<KustoQueryAgent>();
    services.AddTransient<BranchCreationAgent>();
    services.AddTransient<CodeGenerationAgent>();
    services.AddTransient<PRCreationAgent>();
    services.AddTransient<DeploymentMonitorAgent>();

    logger.LogInformation("{Project} services registered with Microsoft Agent Framework", "Infrastructure");

    return services;
  }
}
