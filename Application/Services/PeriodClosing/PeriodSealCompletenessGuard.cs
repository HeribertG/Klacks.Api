// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IPeriodSealCompletenessGuard. Counts first and loads the list only when something is unsealed, so a
/// complete period costs a single count. The list is cut to MaxListedWorks after the database ordered it, and then
/// restricted to the clients the caller may see; the count is not restricted, because a service the caller cannot
/// see still blocks the close.
/// </summary>
/// <param name="workRepository">Counts and lists the unsealed works</param>
/// <param name="rowAccessGuard">Hides the clients the caller may not see from the list</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.PeriodClosing;

namespace Klacks.Api.Application.Services.PeriodClosing;

public sealed class PeriodSealCompletenessGuard : IPeriodSealCompletenessGuard
{
    public const int MaxListedWorks = 20;

    private const int NoUnsealedWorks = 0;

    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;

    public PeriodSealCompletenessGuard(IWorkRepository workRepository, IScheduleRowAccessGuard rowAccessGuard)
    {
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
    }

    public Task<int> CountUnsealedAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default) =>
        _workRepository.CountUnsealedForCloseAsync(startDate, endDate, groupId, cancellationToken);

    public async Task EnsureAllSealedAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default)
    {
        var unsealed = await _workRepository.CountUnsealedForCloseAsync(startDate, endDate, groupId, cancellationToken);
        if (unsealed == NoUnsealedWorks)
        {
            return;
        }

        var rows = await _workRepository.ListUnsealedForCloseAsync(startDate, endDate, groupId, MaxListedWorks, cancellationToken);
        var visible = await _rowAccessGuard.FilterVisibleAsync(rows, row => row.ClientId, _ => null, cancellationToken);

        throw new UnsealedWorksException(unsealed, visible);
    }
}
