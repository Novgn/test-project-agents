# TestProject - AI Agent Workflow System

An intelligent, multi-agent workflow system for automated ETW (Event Tracing for Windows) detector creation using Microsoft Agent Framework, built on Ardalis Clean Architecture principles.

## Overview

An intelligent system that automates the entire lifecycle of ETW detector creation through AI agent orchestration:

1. **User Input** - Accepts ETW provider details via chat interface
2. **Kusto Analysis** - Queries Azure Data Explorer for converters and detectors
3. **Branch Creation** - Automatically creates Azure DevOps branches
4. **PR Analysis** - Reviews historical PRs to learn patterns and conventions
5. **Code Generation** - AI agents generate detector code
6. **Pull Request Creation** - Creates PRs with human-in-the-loop approval
7. **Deployment Monitoring** - Tracks deployment through pipelines
8. **Results Analysis** - Fetches and analyzes detector performance
9. **Customer Rollout** - Creates PRs to make detectors customer-facing

### Architecture

This project follows Clean Architecture principles with a clear separation of concerns:

- **Core** - Domain entities, aggregates, workflow models, and agent interfaces
- **UseCases** - CQRS commands/queries for workflow management
- **Infrastructure** - Agent orchestration, Azure services, data access
- **Web** - FastEndpoints API + SignalR hub for real-time communication
- **FrontEnd** - React + Vite + Fluent UI chat interface
- **ServiceDefaults** - Aspire service defaults

## Project Structure

```
TestProject/
├── src/
│   ├── TestProject.Core/           # Domain model and business logic
│   ├── TestProject.UseCases/       # Application services (CQRS)
│   ├── TestProject.Infrastructure/ # Data access and external services
│   ├── TestProject.Web/            # API endpoints
│   ├── TestProject.ServiceDefaults/# Aspire defaults
│   └── TestProject.AspireHost/     # Aspire orchestration
├── tests/
│   ├── TestProject.UnitTests/      # Unit tests
│   ├── TestProject.IntegrationTests/# Integration tests
│   ├── TestProject.FunctionalTests/# Functional/API tests
│   └── TestProject.AspireTests/    # Aspire integration tests
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server or SQLite

### Running the Application

```bash
dotnet run --project src/TestProject.Web
```

Or run with Aspire:

```bash
dotnet run --project src/TestProject.AspireHost
```

### Running Tests

```bash
dotnet test
```

## Database Migrations

To add a new migration:

```bash
dotnet ef migrations add MigrationName -c AppDbContext -p src/TestProject.Infrastructure/TestProject.Infrastructure.csproj -s src/TestProject.Web/TestProject.Web.csproj -o Data/Migrations
```

To update the database:

```bash
dotnet ef database update -c AppDbContext -p src/TestProject.Infrastructure/TestProject.Infrastructure.csproj -s src/TestProject.Web/TestProject.Web.csproj
```

## Built With

- [Ardalis Clean Architecture](https://github.com/ardalis/CleanArchitecture)
- [FastEndpoints](https://fast-endpoints.com/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR](https://github.com/jbogard/MediatR)
- [Ardalis.Specification](https://github.com/ardalis/specification)
- [Ardalis.Result](https://github.com/ardalis/result)
