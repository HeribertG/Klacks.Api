// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="ListReplacementRequestsQuery"/>. The query must be bounded - an absent employee, a
/// scenario token or a date range of at most ReplacementRequestLimits.MaxListSpanDays - and returns at most
/// MaxListRows rows. Rows are filtered in the database by those bounds, then narrowed to rows whose candidate AND
/// absent employee the caller may see (one visibility query per side); a row touching a hidden employee is simply
/// absent, like a row that does not exist. Admins and background callers are unrestricted by the guard.
/// </summary>
/// <param name="repository">Reads the request book</param>
/// <param name="clientVisibilityGuard">Narrows the rows to visible employees</param>
/// <param name="settingsReader">Short-notice threshold</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class ListReplacementRequestsQueryHandler
    : IRequestHandler<ListReplacementRequestsQuery, IReadOnlyList<ReplacementRequestResource>>
{
    private const string UnboundedMessage =
        "Filter by absentClientId, analyseToken, or a fromDate/untilDate range of at most {0} days.";

    private readonly IReplacementRequestRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ISettingsReader _settingsReader;

    public ListReplacementRequestsQueryHandler(
        IReplacementRequestRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        ISettingsReader settingsReader)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _settingsReader = settingsReader;
    }

    public async Task<IReadOnlyList<ReplacementRequestResource>> Handle(
        ListReplacementRequestsQuery request, CancellationToken cancellationToken)
    {
        EnsureBounded(request);

        var rows = await _repository.ListAsync(
            new ReplacementRequestFilter(request.AbsentClientId, request.FromDate, request.UntilDate, request.AnalyseToken),
            ReplacementRequestLimits.MaxListRows,
            cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var candidateVisible = await _clientVisibilityGuard.FilterVisibleAsync(rows, r => r.CandidateClientId, cancellationToken);
        var visibleRows = await _clientVisibilityGuard.FilterVisibleAsync(candidateVisible, r => r.AbsentClientId, cancellationToken);
        var shortNoticeHours = await ReplacementRequestSettingsReader.ReadShortNoticeHoursAsync(_settingsReader);

        return visibleRows
            .Select(row => ReplacementRequestResourceMapper.ToResource(row, shortNoticeHours))
            .ToList();
    }

    private static void EnsureBounded(ListReplacementRequestsQuery request)
    {
        if (request.AbsentClientId.HasValue || request.AnalyseToken.HasValue)
        {
            return;
        }

        if (request.FromDate is { } from
            && request.UntilDate is { } until
            && until >= from
            && until.DayNumber - from.DayNumber < ReplacementRequestLimits.MaxListSpanDays)
        {
            return;
        }

        throw new InvalidRequestException(string.Format(UnboundedMessage, ReplacementRequestLimits.MaxListSpanDays));
    }
}
