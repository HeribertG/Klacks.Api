# Klacks API

A modern REST API built with .NET 10.0 using Domain-Driven Design (DDD) and Clean Architecture principles.

Dieses Repository wird automatisch auf Hetzner deployed!

## Architecture

### CQRS Pattern with Custom Mediator

The API uses CQRS (Command Query Responsibility Segregation) pattern with a **custom lightweight Mediator implementation**.

**Important:** We do NOT use MediatR. Instead, we have our own implementation:

| Component | Location |
|-----------|----------|
| `IMediator` | `Infrastructure/Mediator/IMediator.cs` |
| `Mediator` | `Infrastructure/Mediator/Mediator.cs` |
| `IRequest<T>` | `Infrastructure/Mediator/IRequest.cs` |
| `IRequestHandler<TRequest, TResponse>` | `Infrastructure/Mediator/IRequestHandler.cs` |
| `Unit` | `Infrastructure/Mediator/Unit.cs` |

Usage example:
```csharp
using Klacks.Api.Infrastructure.Mediator;

public class MyQuery : IRequest<MyResponse> { }

public class MyQueryHandler : IRequestHandler<MyQuery, MyResponse>
{
    public Task<MyResponse> Handle(MyQuery request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### Object Mapping with Mapperly

The API uses **Riok.Mapperly** for compile-time source-generated object mapping.

**Important:** We do NOT use AutoMapper. Mapperly generates mapping code at compile time, resulting in better performance and type safety.

| Mapper | Location |
|--------|----------|
| `ClientMapper` | `Application/Mappers/ClientMapper.cs` |
| `FilterMapper` | `Application/Mappers/FilterMapper.cs` |
| `GroupMapper` | `Application/Mappers/GroupMapper.cs` |
| `ScheduleMapper` | `Application/Mappers/ScheduleMapper.cs` |
| `SettingsMapper` | `Application/Mappers/SettingsMapper.cs` |

Usage example:
```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class MyMapper
{
    // Auto-generated mapping
    public partial TargetDto ToDto(SourceEntity entity);

    // Custom mapping with ignored properties
    [MapperIgnoreTarget(nameof(TargetEntity.NavigationProperty))]
    public partial TargetEntity ToEntity(SourceDto dto);
}
```

Key Mapperly attributes:
- `[Mapper]` - Marks a class as a mapper
- `[MapperIgnoreTarget]` - Ignores a property on the target
- `[MapperIgnoreSource]` - Ignores a property on the source
- `[MapProperty]` - Maps properties with different names

### Project Structure

```
Klacks.Api/
├── Application/
│   ├── Commands/          # CQRS Commands
│   ├── Queries/           # CQRS Queries
│   ├── Handlers/          # Command & Query Handlers
│   ├── Mappers/           # Mapperly Mappers
│   └── Validation/        # FluentValidation Validators
├── Domain/
│   ├── Models/            # Domain Entities
│   ├── Enums/             # Enumerations
│   └── Services/          # Domain Services
├── Infrastructure/
│   ├── Mediator/          # Custom Mediator Implementation
│   ├── Repositories/      # Data Access
│   └── Services/          # Infrastructure Services
└── Presentation/
    ├── Controllers/       # API Controllers
    └── DTOs/              # Data Transfer Objects
```

## Tech Stack

| Technology | Purpose |
|------------|---------|
| .NET 10.0 | Runtime |
| Entity Framework Core | ORM |
| PostgreSQL | Database |
| Riok.Mapperly 4.3.0 | Object Mapping (compile-time) |
| FluentValidation | Input Validation |
| NUnit + FluentAssertions | Unit Testing |
| NSubstitute | Mocking |

## Unit Tests

Tests are located in the `UnitTest` project:

```bash
# Run all tests
dotnet test UnitTest/UnitTest.csproj

# Run specific test category
dotnet test --filter "FullyQualifiedName~Controllers"
```

Current test count: **794 tests**



