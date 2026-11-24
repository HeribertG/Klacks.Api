# Architecture of Klacks.Api

Notes for people who read or change the backend code. For what Klacks does and how to install it, see the [README](../README.md).

## Layers and patterns

The API is a .NET 10 application structured along Clean Architecture and Domain-Driven Design principles, using CQRS with a custom lightweight mediator.

| Layer | May depend on | Contains |
|-------|---------------|----------|
| Presentation | Application, Domain | Controllers, API DTOs |
| Application | Domain | Commands, queries, handlers, mappers, validation |
| Domain | nothing | Entities, enums, interfaces, domain services |
| Infrastructure | Application, Domain | Repositories, database context, external services |

Klacks.Api is a single assembly, so the compiler does not enforce this table. Guard tests in `Klacks.UnitTest/Architecture` do (for example `LayerDependencyGuardTests`): interfaces are defined in Domain or Application and implemented in Infrastructure, and `DataBaseContext` is used in Infrastructure only.

### CQRS with a custom Mediator

**Important:** MediatR is not used. The project has its own implementation:

| Component | Location |
|-----------|----------|
| `IMediator` | `Infrastructure/Mediator/IMediator.cs` |
| `Mediator` | `Infrastructure/Mediator/Mediator.cs` |
| `IRequest<TResponse>` | `Infrastructure/Mediator/IRequest{TResponse}.cs` |
| `IRequest` (no response value) | `Infrastructure/Mediator/IRequest.cs` |
| `IRequestHandler<TRequest, TResponse>` | `Infrastructure/Mediator/IRequestHandler{TRequest,TResponse}.cs` |
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

### Handler, Repository, DataBaseContext

Handlers must never access `DataBaseContext` directly. They use repositories, which access the database context internally.

```
Handler  -->  Repository  -->  DataBaseContext
```

Correct:

```csharp
public class GetDataQueryHandler : IRequestHandler<GetDataQuery, DataResource>
{
    private readonly IDataRepository _repository;
    private readonly DataMapper _mapper;

    public async Task<DataResource> Handle(GetDataQuery request, CancellationToken ct)
    {
        var data = await _repository.GetAsync(request.Id, ct);
        return _mapper.ToResource(data);
    }
}
```

Incorrect:

```csharp
public class GetDataQueryHandler : IRequestHandler<GetDataQuery, DataResource>
{
    private readonly DataBaseContext _context; // direct DB access

    public async Task<DataResource> Handle(GetDataQuery request, CancellationToken ct)
    {
        var data = await _context.Data.FindAsync(request.Id); // wrong
        return new DataResource { ... }; // manual mapping, wrong
    }
}
```

Rules:

1. Handlers inject `IRepository` interfaces, not `DataBaseContext`.
2. Complex operations use facades (for example `IShiftCutFacade`).
3. Mapping is done via Mapperly mappers, not manually in handlers.
4. Most repositories only stage changes; the handler commits them through `IUnitOfWork` (`Domain/Interfaces/IUnitOfWork.cs`). Many handlers derive from `BaseHandler` (`Application/Handlers/BaseHandler.cs`), which wraps the handler body in `ExecuteAsync` for error handling; follow the handlers in the same folder.

### Object mapping with Mapperly

The API uses [Riok.Mapperly](https://github.com/riok/mapperly) for compile-time, source-generated object mapping. AutoMapper is not used.

Examples of mappers in `Application/Mappers/`: `ClientMapper`, `FilterMapper`, `GroupMapper`, `ScheduleMapper`, `SettingsMapper`.

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

- `[Mapper]` marks a class as a mapper
- `[MapperIgnoreTarget]` ignores a property on the target
- `[MapperIgnoreSource]` ignores a property on the source
- `[MapProperty]` maps properties with different names

## Project structure

```
Klacks.Api/
├── Application/
│   ├── Commands/          # CQRS commands
│   ├── Queries/           # CQRS queries
│   ├── Handlers/          # Command and query handlers
│   ├── Interfaces/        # Application interfaces (for example facades)
│   ├── Mappers/           # Mapperly mappers
│   ├── Services/          # Application services
│   └── Validation/        # FluentValidation validators
├── Domain/
│   ├── Models/            # Domain entities
│   ├── Enums/             # Enumerations
│   ├── Interfaces/        # Repository and service interfaces
│   └── Services/          # Domain services
├── Infrastructure/
│   ├── Mediator/          # Custom mediator implementation
│   ├── Persistence/       # Database context, seed data, stored procedures
│   ├── Repositories/      # Data access
│   └── Services/          # Infrastructure services
└── Presentation/
    ├── Controllers/       # API controllers
    └── DTOs/              # Data transfer objects
```

The folders listed above are the core of each layer; the repository contains further folders (for example `Presentation/Mcp`, `Plugins`, `deploy`).

## Tech stack

| Technology | Purpose |
|------------|---------|
| .NET 10 | Runtime |
| Entity Framework Core | ORM |
| PostgreSQL | Database |
| Riok.Mapperly | Object mapping (compile-time) |
| FluentValidation | Input validation |
| NUnit, Shouldly, NSubstitute | Unit testing and mocking |

## Build

The API needs a PostgreSQL database to run. The project references the sibling repositories Klacks.Docs, Klacks.ScheduleOptimizer, Klacks.ScheduleRecovery, Klacks.Api.SourceGenerators, Klacks.Plugin.Contracts and Klacks.Plugin.Messaging; clone them next to Klacks.Api. To build:

```bash
dotnet build Klacks.Api.csproj
```

## Tests

The unit tests live in the separate repository [Klacks.UnitTest](https://github.com/HeribertG/Klacks.UnitTest); its folder `Architecture` holds the guard tests that enforce the layer rules. Endpoint tests against the running application are in [Klacks.ApiTest](https://github.com/HeribertG/Klacks.ApiTest), integration tests in [Klacks.IntegrationTest](https://github.com/HeribertG/Klacks.IntegrationTest).

```bash
# Run all tests
dotnet test Klacks.UnitTest.csproj

# Run a specific test category
dotnet test --filter "FullyQualifiedName~Controllers"
```
