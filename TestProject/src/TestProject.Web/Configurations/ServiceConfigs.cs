using TestProject.Infrastructure;
using TestProject.UseCases;
using TestProject.Core.Interfaces;
using TestProject.Web.Services;

namespace TestProject.Web.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger, WebApplicationBuilder builder)
  {
    services.AddInfrastructureServices(builder.Configuration, logger)
            .AddUseCasesServices(logger)
            .AddMediatrConfigs();

    // Web Services (SignalR-based services)
    services.AddSingleton<IConversationService, ConversationService>();

    logger.LogInformation("{Project} services registered", "Application Services");

    return services;
  }
}
