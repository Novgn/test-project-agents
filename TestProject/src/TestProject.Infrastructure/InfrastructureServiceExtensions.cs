using TestProject.Core.Interfaces;
using TestProject.Infrastructure.Agents;
using TestProject.Infrastructure.Agents.Executors;
using TestProject.Infrastructure.Azure;
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

    // Microsoft Agent Framework Workflow Services
    services.AddSingleton<ETWDetectorWorkflow>();
    services.AddSingleton<IWorkflowOrchestrationService, WorkflowOrchestrationService>();

    // Workflow Executors (must be registered for DI)
    services.AddTransient<KustoQueryExecutor>();
    services.AddTransient<BranchCreationExecutor>();
    services.AddTransient<PRCreationExecutor>();
    services.AddTransient<DeploymentMonitorExecutor>();

    logger.LogInformation("{Project} services registered with Microsoft Agent Framework", "Infrastructure");

    return services;
  }
}
