# Backend Architektur

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

## CQRS mit Custom Mediator

**WICHTIG:** Wir verwenden NICHT MediatR, sondern eine eigene Implementation.

```csharp
using Klacks.Api.Infrastructure.Mediator;

public class MyQuery : IRequest<MyResponse> { }

public class MyQueryHandler : IRequestHandler<MyQuery, MyResponse>
{
    public Task<MyResponse> Handle(MyQuery request, CancellationToken ct)
    {
        // Implementation
    }
}
```

| Component | Location |
|-----------|----------|
| `IMediator` | `Infrastructure/Mediator/IMediator.cs` |
| `Mediator` | `Infrastructure/Mediator/Mediator.cs` |
| `IRequest<T>` | `Infrastructure/Mediator/IRequest.cs` |
| `IRequestHandler` | `Infrastructure/Mediator/IRequestHandler.cs` |

## Handler → Repository → DbContext Pattern

Handlers dürfen **NIEMALS** direkt auf `DataBaseContext` zugreifen!

```
Handler → Repository → DataBaseContext (korrekt)
Handler → DataBaseContext (FALSCH!)
```

**Regeln:**
1. Handlers injizieren `IRepository` Interfaces
2. Komplexe Operationen nutzen Facades (z.B. `IShiftCutFacade`)
3. Mapping über Mapperly Mapper
4. Domain Services werden von Repositories/Facades koordiniert

## Mapperly (NICHT AutoMapper!)

**WICHTIG:** Wir verwenden NICHT AutoMapper, sondern Riok.Mapperly.

```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class MyMapper
{
    public partial TargetDto ToDto(SourceEntity entity);

    [MapperIgnoreTarget(nameof(TargetEntity.NavigationProperty))]
    public partial TargetEntity ToEntity(SourceDto dto);
}
```

| Mapper | Location |
|--------|----------|
| `ClientMapper` | `Application/Mappers/ClientMapper.cs` |
| `FilterMapper` | `Application/Mappers/FilterMapper.cs` |
| `GroupMapper` | `Application/Mappers/GroupMapper.cs` |
| `ScheduleMapper` | `Application/Mappers/ScheduleMapper.cs` |
| `SettingsMapper` | `Application/Mappers/SettingsMapper.cs` |

## Projektstruktur

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
│   ├── Mediator/          # Custom Mediator
│   ├── Repositories/      # Data Access
│   └── Services/          # Infrastructure Services
└── Presentation/
    ├── Controllers/       # API Controllers
    └── DTOs/              # Data Transfer Objects
```

## Unit Tests

```bash
# Alle Tests ausführen
dotnet test UnitTest/UnitTest.csproj

# Spezifische Kategorie
dotnet test --filter "FullyQualifiedName~Controllers"
```

Aktuell: **~800 Tests**
