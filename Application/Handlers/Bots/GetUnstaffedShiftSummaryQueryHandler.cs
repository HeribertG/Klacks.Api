// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the read-only bot query "how many shift-days were understaffed for this client
/// in this date range". Resolves the client by a name fragment, then counts shift-day
/// assignments that satisfy UnstaffedShiftPredicate.IsUnstaffed for that client's shifts.
/// Deliberately never lists matching client names in error messages: the bot token has no
/// user role and is reachable through channels an untrusted sender could reach (Telegram,
/// Discord, email), so an ambiguous-name response must not become a client directory oracle.
/// </summary>
/// <param name="request">Contains the free-text client name fragment and the date range</param>

using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Bots;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Bots;

public class GetUnstaffedShiftSummaryQueryHandler : IRequestHandler<GetUnstaffedShiftSummaryQuery, UnstaffedShiftSummaryDto>
{
    private const int MaxDateRangeDays = 90;
    private const int FilterRowCount = 5000;

    private readonly IClientRepository _clientRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;

    public GetUnstaffedShiftSummaryQueryHandler(
        IClientRepository clientRepository,
        IShiftRepository shiftRepository,
        IShiftScheduleRepository shiftScheduleRepository)
    {
        _clientRepository = clientRepository;
        _shiftRepository = shiftRepository;
        _shiftScheduleRepository = shiftScheduleRepository;
    }

    public async Task<UnstaffedShiftSummaryDto> Handle(GetUnstaffedShiftSummaryQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            throw new InvalidRequestException("Client name must not be empty.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new InvalidRequestException("End date must not be before start date.");
        }

        if (request.EndDate.DayNumber - request.StartDate.DayNumber > MaxDateRangeDays)
        {
            throw new InvalidRequestException($"Date range must not exceed {MaxDateRangeDays} days.");
        }

        var matches = await _clientRepository.SearchByNameAsync(request.ClientName, cancellationToken);

        // Deliberately identical message for zero and multiple matches: distinguishing "does not
        // exist" from "exists more than once" would let a caller iteratively narrow a name
        // fragment and confirm real client names one character at a time.
        if (matches.Count != 1)
        {
            throw new InvalidRequestException("Client not found or ambiguous. Please provide a more specific name.");
        }

        var client = matches[0];
        var shiftIds = await _shiftRepository.GetShiftIdsByClientAsync(client.Id, cancellationToken);
        if (shiftIds.Count == 0)
        {
            return new UnstaffedShiftSummaryDto(client.Id, client.Name, request.StartDate, request.EndDate, 0);
        }

        var shiftIdSet = shiftIds.ToHashSet();
        var filter = new ShiftScheduleFilter
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsSporadic = true,
            IsTimeRange = true,
            Container = true,
            IsStandartShift = true,
            ShowUngroupedShifts = true,
            RowCount = FilterRowCount
        };

        var (assignments, totalCount) = await _shiftScheduleRepository.GetShiftScheduleAsync(filter, cancellationToken);
        if (totalCount > assignments.Count)
        {
            // GetShiftScheduleAsync paginates internally (Skip/Take over RowCount); silently
            // reporting a count derived from a truncated page would understate the real number
            // instead of failing loudly. FilterRowCount is generous for a 90-day window, so this
            // should not trigger in practice -- if it does, surface it rather than guess.
            throw new InvalidRequestException("Too many shift assignments in this date range to compute an exact count. Please use a shorter range.");
        }

        var unstaffedCount = assignments.Count(a => shiftIdSet.Contains(a.ShiftId) && UnstaffedShiftPredicate.IsUnstaffed(a));

        return new UnstaffedShiftSummaryDto(client.Id, client.Name, request.StartDate, request.EndDate, unstaffedCount);
    }
}
