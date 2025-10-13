using Ardalis.SharedKernel;
using System.Reflection;
using TestProject.Core.AgentWorkflowAggregate;

namespace TestProject.Web.Configurations;

public static class MediatrConfigs
{
  public static IServiceCollection AddMediatrConfigs(this IServiceCollection services)
  {
    var mediatRAssemblies = new[]
      {
        Assembly.GetAssembly(typeof(IRepository<>)), // Core (SharedKernel)
        Assembly.GetAssembly(typeof(ETWInput)), // Core
        Assembly.GetExecutingAssembly() // Web
      };

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(mediatRAssemblies!))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
            .AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

    return services;
  }
}
