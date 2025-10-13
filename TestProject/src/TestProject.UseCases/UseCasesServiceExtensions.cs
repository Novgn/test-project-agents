using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestProject.UseCases.Workflows.Chat;

namespace TestProject.UseCases;

public static class UseCasesServiceExtensions
{
  public static IServiceCollection AddUseCasesServices(
    this IServiceCollection services,
    ILogger logger)
  {
    // Application Services (Use Cases)
    services.AddSingleton<ConversationalChatService>();

    logger.LogInformation("{Project} services registered", "UseCases");

    return services;
  }
}
