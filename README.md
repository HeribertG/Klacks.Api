# Klacks

[![License: AGPL-3.0](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)

Klacks is open-source workforce scheduling for shift and field operations, for example home care, hospitals, security, cleaning services, logistics and hospitality. You run it on your own infrastructure.

This repository contains the backend (`Klacks.Api`, .NET). The web interface lives in a separate repository, [Klacks.Ui](https://github.com/HeribertG/Klacks.Ui).

- Website: <https://klacks-software.ch>
- Documentation: <https://klacks-software.ch/docs/>
- Playground (public instance with sample data, no installation, no registration): <https://play.klacks-software.ch>
- Installation page: <https://klacks-software.ch/en/land-gb/installation>
- Download: [klacks-onprem.zip](https://github.com/HeribertG/Klacks.Api/releases/latest/download/klacks-onprem.zip) from the [latest GitHub release](https://github.com/HeribertG/Klacks.Api/releases/latest)

## What Klacks does

- **Automatic scheduling.** Builds a schedule for you using an evolutionary algorithm, taking into account working-time limits, qualifications and availability.
- **Rule checks.** Flags breaches of rest periods, qualifications, public holidays and surcharges before a schedule goes out. By default Klacks only warns; a blocking mode can be enabled per rule type and applies in automatic planning, while manual planning continues to only warn. Surcharges are handled as time credits in hours, not as money.
- **Route and tour optimization.** For mobile operations, using an ant colony algorithm (by car, bike, on foot or mixed).
- **Holidays and rules per country.** Public holidays and rules can be set up per country, region and municipality, and several calendars can be mixed.
- **AI assistant "Klacksy".** Controls the application in natural language, typed or spoken. You choose the language model yourself, from external providers (OpenAI, Anthropic, DeepSeek, Gemini) to a model you run locally.
- **MCP endpoint.** Klacks speaks the Model Context Protocol, so external AI agents can work with it using the rights of the connected user.
- **25 languages.** German, French, Italian and English are built in; 21 more ship as language packs that you install in the settings.
- **Playground.** A public instance with sample data to try it without installing anything.

### What Klacks does not do

Klacks is a planning tool. According to the project's own comparison page it has no biometric or fingerprint recognition, no live GPS tracking, no time clock or kiosk terminal, and no payroll accounting (it exports formatted data, for example for DATEV, but does not process payroll).

### Data and AI

Klacks runs on your own servers, and staff and scheduling data are stored in your own PostgreSQL database. If you choose an external (cloud) AI provider for Klacksy, requests, and with them planning data, are sent to that provider. With a locally run model, everything stays in-house.

## Quick start (on-premise, Docker)

Requirements, as stated in the bundled installation guide:

- A host with at least 8 GB RAM and 4 vCPU. The API runs its retrieval models locally.
- Docker Desktop (Windows/Mac) or Docker Engine with the Compose plugin (Linux).
- Outbound access to `ghcr.io` and `github.com` (images and updates).
- Ports 80 and 443 free (configurable).

Download [klacks-onprem.zip](https://github.com/HeribertG/Klacks.Api/releases/latest/download/klacks-onprem.zip), extract it, and run the installer from the extracted `onprem` folder.

Windows (PowerShell):

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -ServerName klacks.example.com -Region de
```

Linux:

```bash
SERVER_NAME=klacks.example.com REGION=de ./install.sh
```

The installer generates secrets and a self-signed certificate, pins the latest released version, starts the stack and waits until it is healthy. `-Region` / `REGION` is optional and pre-configures a country's locale, holidays, working-time limits, surcharges and industry presets on first boot; the available country codes are the files in [`deploy/onprem/regions`](deploy/onprem/regions). On Windows, install into a folder on a local drive (for example `C:\klacks`).

After the first start, log in from your browser and change the initial password immediately. It is listed in the bundled [`README.md`](deploy/onprem/README.md). Updates, rollback, backup and your own certificate are covered in the [documentation](https://klacks-software.ch/docs/).

## License

Klacks is licensed under the [GNU Affero General Public License v3.0 only](LICENSE) (AGPL-3.0-only) since 2026-09-28. Versions released up to and including v1.0.35 were published under the MIT license.

There are no license fees. Hosting and operation (servers, maintenance, and an external AI model if you use one) still cost money; what you do not pay is a fee per employee.

If you want to build Klacks into a closed product or keep your changes private, a commercial license is available; see the [license page](https://klacks-software.ch/en/land-gb/lizenz).

## About the project

Klacks is developed by Heribert Gasparoli in Liebefeld (Switzerland). The community that is meant to carry the further development is, according to the project's about page, still at the beginning. See [About](https://klacks-software.ch/en/land-gb/ueber-uns).

## For developers

### Architecture

The API is a .NET 10 application structured along Clean Architecture and Domain-Driven Design principles, using CQRS with a custom lightweight mediator.

#### CQRS with a custom Mediator

**Important:** MediatR is not used. The project has its own implementation:

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

#### Handler, Repository, DataBaseContext

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
4. Domain services are coordinated by repositories or facades, not by handlers.

#### Object mapping with Mapperly

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

### Project structure

```
Klacks.Api/
├── Application/
│   ├── Commands/          # CQRS commands
│   ├── Queries/           # CQRS queries
│   ├── Handlers/          # Command and query handlers
│   ├── Mappers/           # Mapperly mappers
│   └── Validation/        # FluentValidation validators
├── Domain/
│   ├── Models/            # Domain entities
│   ├── Enums/             # Enumerations
│   └── Services/          # Domain services
├── Infrastructure/
│   ├── Mediator/          # Custom mediator implementation
│   ├── Repositories/      # Data access
│   └── Services/          # Infrastructure services
└── Presentation/
    ├── Controllers/       # API controllers
    └── DTOs/              # Data transfer objects
```

The folders listed above are the core of each layer; the repository contains further folders (for example `Infrastructure/MCP`, `Presentation/Mcp`, `Plugins`, `deploy`).

### Tech stack

| Technology | Purpose |
|------------|---------|
| .NET 10 | Runtime |
| Entity Framework Core | ORM |
| PostgreSQL | Database |
| Riok.Mapperly | Object mapping (compile-time) |
| FluentValidation | Input validation |
| NUnit, Shouldly, NSubstitute | Unit testing and mocking |

### Build

The API needs a PostgreSQL database to run. To build:

```bash
dotnet build Klacks.Api.csproj
```

### Tests

The unit tests live in the separate repository [Klacks.UnitTest](https://github.com/HeribertG/Klacks.UnitTest):

```bash
# Run all tests
dotnet test Klacks.UnitTest.csproj

# Run a specific test category
dotnet test --filter "FullyQualifiedName~Controllers"
```
