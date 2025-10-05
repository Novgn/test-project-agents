var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TestProject_Web>("web");

builder.Build().Run();
