# TestProject

A Clean Architecture ASP.NET Core project built using the Ardalis Clean Architecture template.

## Overview

This project follows Clean Architecture principles with a clear separation of concerns:

- **Core** - Domain model, entities, aggregates, and business logic
- **UseCases** - Application services implementing CQRS commands and queries
- **Infrastructure** - Data access, external services, and infrastructure concerns
- **Web** - API endpoints using FastEndpoints
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
