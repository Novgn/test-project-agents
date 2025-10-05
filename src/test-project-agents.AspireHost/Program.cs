var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.test_project_agents_Web>("web");

builder.Build().Run();
