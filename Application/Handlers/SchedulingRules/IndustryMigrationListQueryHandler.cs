// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the contracts still bound to a scheduling rule of an inactive industry, so an admin can
/// migrate them deliberately after switching ACTIVE_INDUSTRIES. Nothing is changed here - the list
/// exists precisely because contracts are never rewritten automatically.
/// </summary>
/// <param name="migrationReader">Supplies the affected contracts and their client counts</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Scheduling;
using Klacks.Api.Application.Queries.SchedulingRules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class IndustryMigrationListQueryHandler
    : BaseHandler, IRequestHandler<IndustryMigrationListQuery, IReadOnlyList<IndustryMigrationCandidate>>
{
    private readonly IIndustryMigrationReader _migrationReader;

    public IndustryMigrationListQueryHandler(
        IIndustryMigrationReader migrationReader,
        ILogger<IndustryMigrationListQueryHandler> logger)
        : base(logger)
    {
        _migrationReader = migrationReader;
    }

    public async Task<IReadOnlyList<IndustryMigrationCandidate>> Handle(
        IndustryMigrationListQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () => await _migrationReader.GetContractsOnInactiveIndustriesAsync(cancellationToken),
            "loading the industry migration list",
            new { });
    }
}
