// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the shift group-visibility rule to container templates and overrides: the container must exist and be
/// visible, and new task shifts of a write must be visible. Unchanged task shifts the container already references stay
/// allowed, so the stored baseline is read only when the requested set contains a hidden or missing shift.
/// </summary>
/// <param name="shiftVisibilityGuard">Shared shift visibility rule (existence and visibility of shift ids)</param>
/// <param name="shiftVisibilityReader">Reads the task shifts the container already references</param>
/// <param name="logger">Logs refused writes</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerVisibilityGuard : IContainerVisibilityGuard
{
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IShiftVisibilityReader _shiftVisibilityReader;
    private readonly ILogger<ContainerVisibilityGuard> _logger;

    public ContainerVisibilityGuard(
        IShiftVisibilityGuard shiftVisibilityGuard,
        IShiftVisibilityReader shiftVisibilityReader,
        ILogger<ContainerVisibilityGuard> logger)
    {
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftVisibilityReader = shiftVisibilityReader;
        _logger = logger;
    }

    public async Task EnsureContainerVisibleAsync(Guid containerId, CancellationToken cancellationToken = default)
    {
        if (!await _shiftVisibilityGuard.ExistsAndVisibleAsync(containerId, cancellationToken))
        {
            _logger.LogWarning("Container request refused on hidden or missing container {ContainerId}", containerId);
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, containerId));
        }
    }

    public async Task EnsureItemShiftsVisibleAsync(
        Guid containerId, IEnumerable<Guid?> requestedShiftIds, CancellationToken cancellationToken = default)
    {
        var requested = requestedShiftIds
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (requested.Count == 0)
        {
            return;
        }

        var visible = await _shiftVisibilityGuard.FilterVisibleAsync(requested, cancellationToken);
        var notVisible = requested.Where(id => !visible.Contains(id)).ToList();
        if (notVisible.Count == 0)
        {
            return;
        }

        var stored = (await _shiftVisibilityReader.GetContainerItemShiftIdsAsync(containerId, cancellationToken)).ToHashSet();
        var firstNew = notVisible.FirstOrDefault(id => !stored.Contains(id));
        if (firstNew == Guid.Empty)
        {
            return;
        }

        _logger.LogWarning(
            "Container write refused: task shift {ShiftId} is hidden or missing for container {ContainerId}", firstNew, containerId);
        throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, firstNew));
    }
}
