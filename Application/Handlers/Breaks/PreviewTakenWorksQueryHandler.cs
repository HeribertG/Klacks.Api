// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names the works a bulk absence would take away, before anything is written. Absences for a person or scenario outside
/// the caller's visibility are refused exactly like the bulk entry refuses them.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the people and the scenario of the absences</param>
/// <param name="workRemovalService">The rule which works an absence takes away</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PreviewTakenWorksQueryHandler : IRequestHandler<PreviewTakenWorksQuery, IReadOnlyList<RemovableWork>>
{
    private const string ClientsNotFoundMessage = "One or more clients of the request were not found";

    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IAbsenceWorkRemovalService _workRemovalService;

    public PreviewTakenWorksQueryHandler(IScheduleRowAccessGuard rowAccessGuard, IAbsenceWorkRemovalService workRemovalService)
    {
        _rowAccessGuard = rowAccessGuard;
        _workRemovalService = workRemovalService;
    }

    public async Task<IReadOnlyList<RemovableWork>> Handle(PreviewTakenWorksQuery request, CancellationToken cancellationToken)
    {
        if (request.Request.Breaks.Count > RateLimitingPolicies.MaxBulkOperationItems)
        {
            throw new ArgumentException($"Maximum {RateLimitingPolicies.MaxBulkOperationItems} absences per request.");
        }

        var taken = new List<RemovableWork>();
        foreach (var scenario in request.Request.Breaks.GroupBy(item => item.AnalyseToken))
        {
            var clientIds = scenario.Select(item => item.ClientId).Distinct().ToList();
            if (!await _rowAccessGuard.AreAllVisibleAsync(clientIds, scenario.Key, cancellationToken))
            {
                throw new KeyNotFoundException(ClientsNotFoundMessage);
            }

            var absences = scenario
                .Select(item => new AbsenceSpan(item.ClientId, item.AbsenceId, item.CurrentDate, item.StartTime, item.EndTime))
                .ToList();
            taken.AddRange(await _workRemovalService.FindAsync(absences, scenario.Key, cancellationToken));
        }

        return taken;
    }
}
