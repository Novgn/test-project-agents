using Ardalis.ListStartupServices;
using TestProject.Infrastructure.Agents;
using TestProject.Infrastructure.Data;

namespace TestProject.Web.Configurations;

public static class MiddlewareConfig
{
  public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app)
  {
    // CORS must be first in the pipeline
    app.UseCors("AllowFrontend");

    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseShowAllServicesMiddleware(); // see https://github.com/ardalis/AspNetCoreStartupServices
    }
    else
    {
      app.UseDefaultExceptionHandler(); // from FastEndpoints
      app.UseHsts();
    }

    // Configure FastEndpoints with global CORS
    app.UseFastEndpoints(config =>
        {
          config.Endpoints.Configurator = ep =>
          {
            ep.Options(b => b.RequireCors("AllowFrontend"));
          };
        })
        .UseSwaggerGen(); // Includes AddFileServer and static files middleware

    // Map SignalR hub for real-time conversation messages with CORS
    app.MapHub<ConversationHub>("/hubs/conversation")
       .RequireCors("AllowFrontend");

    // Only use HTTPS redirection in production (it can interfere with CORS in development)
    if (!app.Environment.IsDevelopment())
    {
      app.UseHttpsRedirection();
    }

    await SeedDatabase(app);

    return app;
  }

  static async Task SeedDatabase(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
      var context = services.GetRequiredService<AppDbContext>();
      //          await context.Database.MigrateAsync();
      await context.Database.EnsureCreatedAsync();
      await SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
    }
  }
}
