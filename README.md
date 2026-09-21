# Klacks API

[DeepWiki documentation for this repository](https://deepwiki.com/HeribertG/Klacks.Api)
[Klacks product website](https://klacks-software.ch)
[![Tests](https://github.com/HeribertG/Klacks.Api/actions/workflows/tests.yml/badge.svg)](https://github.com/HeribertG/Klacks.Api/actions/workflows/tests.yml)
[![CodeQL](https://github.com/HeribertG/Klacks.Api/actions/workflows/codeql.yml/badge.svg)](https://github.com/HeribertG/Klacks.Api/actions/workflows/codeql.yml)

Backend of the Klacks ecosystem for workforce scheduling and deployment planning
(*Personaleinsatzplanung*): a REST API with a SignalR real-time layer, the scheduling domain
(shifts, schedule entries, breaks, absences, expenses, macros/surcharges), staff and client master
data, the **Klacksy** assistant with pluggable LLM providers, an email/IMAP subsystem, a plugin and
language system, a knowledge index for grounded answers, and an MCP endpoint for external AI clients.

Built with **.NET 10** (`net10.0`), **CQRS with a custom mediator**, **EF Core + PostgreSQL**,
**Riok.Mapperly** for compile-time mapping and **FluentValidation** for input validation, and
deployed as containers to a Hetzner Cloud server.

> Architecture documentation for this repository is the **DeepWiki** linked above, generated from
> this repo. To steer what it documents, see [`.devin/wiki.json`](.devin/wiki.json). Hand-maintained
> topic notes live in [`.claude/skills/`](.claude/skills/), release gating in
> [`docs/RELEASE-CHECKLIST.md`](docs/RELEASE-CHECKLIST.md) and the MCP endpoint in
> [`Presentation/Mcp/README.md`](Presentation/Mcp/README.md).

## Tech stack

Versions as declared in `Klacks.Api.csproj`.

| Area | Technology |
| --- | --- |
| Runtime | .NET 10 (`net10.0`, `Nullable` + `ImplicitUsings` enabled) |
| ORM / DB | EF Core `10.0.11`, `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3`, `EFCore.NamingConventions` `10.0.1`, `Npgsql.Json.NET` `10.0.3` |
| CQRS | Own mediator (`Infrastructure/Mediator/`), no MediatR |
| Mapping | `Riok.Mapperly` `4.3.1` (source-generated), no AutoMapper |
| Validation | `FluentValidation` `12.1.1` (+ DI extensions) |
| Real-time | ASP.NET Core SignalR (`Microsoft.AspNetCore.SignalR.StackExchangeRedis` `10.0.11`) |
| Authentication | `JwtBearer` `10.0.11`, `Identity.EntityFrameworkCore` `10.0.11`, `System.DirectoryServices.Protocols` `10.0.11` (LDAP/AD) |
| API surface | `Microsoft.AspNetCore.OpenApi` `10.0.11`, `Scalar.AspNetCore` `2.17.1` |
| MCP | `ModelContextProtocol.AspNetCore` `2.2.0` |
| Email | `MailKit` `4.17.0` (IMAP/SMTP) |
| Embeddings | `Microsoft.ML.OnnxRuntime` `1.29.0`, `Microsoft.ML.Tokenizers` `2.0.0`, `Tokenizers.DotNet` `1.4.1` (linux-x64, linux-arm64, win-arm64 runtimes) |
| Reporting / export | `ClosedXML` `0.105.1`, `System.IO.Packaging` `10.0.11` |
| Payments | `Stripe.net` `52.3.0` |
| Scheduling helpers | `Cronos` `0.13.0` |
| Build / analysis | `StyleCop.Analyzers` `1.1.118`, `Microsoft.VSSDK.BuildTools` `18.5.40034` |

## Repository layout

```
Application/     Commands, Queries, Handlers, DTOs, Interfaces, Mappers, Services, Skills, Klacksy,
                 Constants, Validation, Exceptions, Helpers, Configuration
Domain/          Models, DTOs, Entities-Interfaces, Enums, ValueObjects, Services, Specifications,
                 Events, Attributes, Constants, Exceptions, Extensions, Helpers, Logging, Security
Infrastructure/  Mediator, Persistence (+ Migrations), Repositories, Services, Email, Hubs, MCP,
                 Plugins, Authentication, Middleware, Security, Scripting, WebSearch, FileHandling,
                 Converters, Events, Extensions, Interfaces, StartupChecks
Presentation/    Controllers (Assistant, UserBackend, Web, OAuth, Bots, Plugins, Internal), DTOs,
                 Mcp, Filters, Extensions
KnowledgeIndex/  Application / Domain / Infrastructure (Onnx, Api, Persistence) / Presentation /
                 Snapshot — retrieval and embedding pipeline
Plugins/         Features (floor-plan, messaging, payroll-export-de) and Languages (21 locales)
GeoData/         Regions (region data, e.g. CH.json)
deploy/          docker-compose-server.yml, nginx-proxy, db-scripts, onprem/, entrypoint.sh
docs/            RELEASE-CHECKLIST.md, diagnostics/*.sql
scripts/         PowerShell maintenance and turn-eval scripts
.claude/skills/  Hand-maintained architecture/deployment topic notes
```

The solution (`Klacks.Api.sln`) also contains **sibling repositories**, referenced by relative path:
`Klacks.UnitTest`, `Klacks.ApiTest`, `Klacks.IntegrationTest`, `Klacks.E2ETest`, `Klacks.Blazor`,
`Klacks.Docs`, `Klacks.ScheduleOptimizer`, `Klacks.ScheduleRecovery`, `Klacks.Api.SourceGenerators`,
`Klacks.Plugin.Contracts`, `Klacks.Plugin.Messaging`. They are **not** part of this repository and not
used as git submodules — a full solution build requires them to be checked out next to this one
(that is what `Dockerfile` and the CI workflows do).

## Architecture rules

### CQRS with a custom mediator

The API uses CQRS with its own lightweight mediator. **MediatR is not used.**

| Component | Location |
| --- | --- |
| `IMediator`, `Mediator` | `Infrastructure/Mediator/IMediator.cs`, `Infrastructure/Mediator/Mediator.cs` |
| `IRequest<T>`, `IRequestHandler<TRequest, TResponse>` | `Infrastructure/Mediator/IRequest.cs`, `Infrastructure/Mediator/IRequestHandler.cs` |
| `IPipelineBehavior` | `Infrastructure/Mediator/IPipelineBehavior.cs` |
| `Unit`, DI registration | `Infrastructure/Mediator/Unit.cs`, `Infrastructure/Mediator/ServiceCollectionExtensions.cs` |

```csharp
using Klacks.Api.Infrastructure.Mediator;

public class MyQuery : IRequest<MyResponse> { }

public class MyQueryHandler : IRequestHandler<MyQuery, MyResponse>
{
    public Task<MyResponse> Handle(MyQuery request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
```

### Handler → Repository → DataBaseContext

Handlers must **never** touch `DataBaseContext` directly; they go through repository interfaces, and
mapping goes through Mapperly-generated mappers.

```
Handler ──▶ Repository ──▶ DataBaseContext
```

Rules:

1. Handlers inject `IRepository` interfaces, not `DataBaseContext`.
2. Complex operations use facades (for example `IShiftCutFacade`).
3. Mapping is done with Mapperly mappers (`Application/Mappers/`), never manually in handlers.
4. Domain services are coordinated by repositories/facades, not by handlers.

### Object mapping with Mapperly

`Riok.Mapperly` generates mapping code at compile time (**AutoMapper is not used**). Mappers live in
`Application/Mappers/` — for example `ClientMapper`, `FilterMapper`, `GroupMapper`, `HolidayWorkExemptionMapper`,
`SkillMapper`, `SpamRuleMapper` and the `ScheduleMapper` partials (`ScheduleMapper.Shifts.cs`,
`ScheduleMapper.Containers.cs`, `ScheduleMapper.Notifications.cs`, …).

Key attributes: `[Mapper]`, `[MapperIgnoreTarget]`, `[MapperIgnoreSource]`, `[MapProperty]`.

## Feature surface

- **Scheduling domain** — shifts and shift cuts, schedule entries, work entries, breaks, absences,
  expenses, container templates, schedule timeline, macro engine and surcharge calculation, period
  hours and period closing, schedule validation.
- **Staff & clients** — clients, groups (incl. visibility/hierarchy), staff, contracts, memberships,
  associations, client availability, address book, route optimization and group geocoding.
- **Klacksy assistant** — multi-turn execution, LLM provider abstraction with implementations for
  Anthropic, Azure, DeepSeek, Gemini, Mistral, OpenAI and a generic provider (`Infrastructure/Services/Assistant/Providers/`),
  a skills system (`Application/Skills/`, 391 C# files), assistant memory (`AgentMemory`), agent
  triggers, escalation chains, goal reflection and learning background services.
- **Knowledge index** — `KnowledgeIndex/` embeddings (ONNX runtime, Gemini/OpenAI embedding providers),
  retrieval and snapshot export; PostgreSQL `pgvector` upgrade script in `deploy/db-scripts/`.
- **MCP server** — authenticated Streamable-HTTP endpoint `POST /mcp` (OAuth 2.1 resource metadata,
  JWT bearer, personal access tokens) so external AI clients can drive the application; see
  `Presentation/Mcp/README.md`. A client side lives in `Infrastructure/MCP/`.
- **Auth & identity** — JWT bearer, ASP.NET Identity, LDAP/Active Directory and OAuth2 identity
  providers, personal access tokens, roles/permissions.
- **Email** — IMAP polling background service, email folders, received-email management, spam rules,
  SMTP sending and email tests, per-client email assignment.
- **Plugins & localization** — feature plugins under `Plugins/Features/` (floor plan, messaging,
  payroll export DE) against the contracts from the `Klacks.Plugin.Contracts` sibling repo; 21
  language plugins under `Plugins/Languages/` plus multi-language JSONB fields and translation services.
- **Reports & data exchange** — report templates, floor plans and work markers, exports and imports,
  ERP order import and ERP object storage, Stripe-based donations.
- **Real-time** — SignalR hubs: `WizardJobHub`, `AutoWizardJobHub`, `HarmonizerJobHub`,
  `HolisticHarmonizerJobHub`, `WorkNotificationHub`, `AssistantNotificationHub`, `EmailNotificationHub`.
- **Configuration & background work** — 22 sections in `appsettings.json`, 20 toggles under
  `BackgroundServices`, backed by 31 `BackgroundService` implementations (e.g. `EmailPolling`,
  `MemoryCleanup`, `Embedding`, `KlacksyLearning`, `PeriodHours`, `ScheduleTimeline`,
  `GroupGeocoding`, `KnowledgeIndexStartup`, `AnswerGroundingSentinel`, `ErpOrderImport`).
- **Regions & on-premise** — `region-setup.dev.json` (region, languages, locale, calendar, worktime,
  surcharges, export, compliance, demo data, industry profiles), region packages with signed
  updates (`Update.ManifestBaseUrl`, `SignaturePublicKey`, `RequireSignedRegionPackages`) and a
  self-hosted stack in `deploy/onprem/` (`install.sh`, `install.ps1`, nginx, init scripts, regions).

## Requirements

- **.NET SDK 10.0.x** (`TargetFramework net10.0`; CI and `Dockerfile` both pin 10.0).
- **PostgreSQL** (Docker Compose puts it on port 5432 inside the compose network). The development
  connection string points at `localhost:5434`, database `klacks_fresh`.
- The **sibling repositories** listed above, checked out next to this repository, for a full
  solution build.
- A JWT secret: `JwtSettings.Secret` is **not** committed for production — provide it via user
  secrets or environment variables (`appsettings.Development.json` carries the placeholder
  `REPLACE_VIA_USER_SECRETS_OR_ENV`).

## Getting started

```bash
# database
docker compose up -d postgres          # or use an existing PostgreSQL instance

# build and run the API
dotnet restore Klacks.Api/Klacks.Api.csproj
dotnet run --project Klacks.Api/Klacks.Api.csproj
```

The launch profile serves `https://localhost:5001` / `http://localhost:5000` and opens the Scalar
API reference at `/scalar/v1`.

Database schema changes use EF Core migrations (`Infrastructure/Persistence/Migrations/`, 112
migrations in this revision):

```bash
dotnet ef migrations add <Name> --project Klacks.Api/Klacks.Api.csproj
```

## Tests and CI

**This repository contains no test project** — tests live in dedicated sibling repositories:

| Repository | Kind |
| --- | --- |
| `Klacks.UnitTest` | unit tests (InMemory provider), NSubstitute |
| `Klacks.ApiTest` | boots the real `Program.cs` via `WebApplicationFactory` against a live PostgreSQL |
| `Klacks.IntegrationTest` | larger integration suite against a live PostgreSQL |
| `Klacks.E2ETest` | end-to-end suite |
| `Klacks.Ui` | frontend tests |

`.github/workflows/tests.yml` runs three backend jobs (`backend-tests`, `api-tests`,
`integration-tests`) on every push and pull request to `main`; it checks out exactly the sibling
repos each project reference needs. The databases used by `Klacks.ApiTest` and
`Klacks.IntegrationTest` are addressed with hardcoded connection strings
(`Host=localhost;Port=5434;Database=klacks;User=postgres;Password=admin`), so the CI service
container is shaped to match them. LLM-dependent and other non-deterministic tests are excluded
from CI and are gated separately by [`docs/RELEASE-CHECKLIST.md`](docs/RELEASE-CHECKLIST.md) before
a `vX.Y.Z` tag (a tag triggers the production deploy plus migrations).

Other workflows: `deploy.yml` (checks out the eight sibling repos it needs, builds, runs the unit
suite, deploys to the Hetzner server), `reset-and-deploy.yml`, `codeql.yml`, `diagnose.yml`.

## Deployment

- `Dockerfile` — multi-stage build on `mcr.microsoft.com/dotnet/sdk:10.0`, embedding version
  arguments (`KLACKS_VERSION_*`, `KLACKS_BUILD_KEY`, `KLACKS_VARIANT`) and copying the sibling
  projects it needs.
- `docker-compose.yml` — services `postgres`, `pgadmin`, `klacks-api`, `klacks-ui`, `klacks-blazor`,
  `klacks-marketplace`, `nginx-proxy`.
- `deploy/docker-compose-server.yml`, `deploy/nginx-proxy/`, `deploy/entrypoint.sh`,
  `deploy/db-scripts/` (incl. `upgrade_to_pgvector.sql`), `deploy/whisper-stt-provider.sql`.
- `deploy/onprem/` — self-hosted installation (compose file, nginx, init scripts, install scripts
  for Linux and Windows, region data).
- Server details (Hetzner Cloud host, container ports behind nginx) are documented in
  [`.claude/skills/deployment.md`](.claude/skills/deployment.md).

## Licence

MIT — see [`LICENSE`](LICENSE) (`Copyright (c) 2025-2026 Heribert Gasparoli`). Note that most source
files carry a `Copyright (c) Heribert Gasparoli Private. All rights reserved.` header, which does not
match the MIT text in `LICENSE`.
