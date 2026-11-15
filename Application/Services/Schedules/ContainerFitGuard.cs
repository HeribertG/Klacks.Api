// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies ContainerFitCheck to a container template or day override with the TimeRange flag of each item shift read
/// from the database, never from the request: the time an item occupies is decided the way ContainerWorkExpansionService
/// builds the child work, so the check follows the rule of ContainerWorkExpansionService.
/// </summary>
/// <param name="shiftReader">Reads which task shifts of the items are TimeRange shifts</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerFitGuard : IContainerFitGuard
{
    private readonly IShiftReader _shiftReader;

    public ContainerFitGuard(IShiftReader shiftReader)
    {
        _shiftReader = shiftReader;
    }

    public async Task EnsureTemplateFitsAsync(ContainerTemplate template, CancellationToken cancellationToken = default)
    {
        var timeRangeShiftIds = await LoadTimeRangeShiftIdsAsync(
            template.ContainerTemplateItems.Select(item => item.ShiftId), cancellationToken);
        ContainerFitCheck.EnsureTemplateFits(template, timeRangeShiftIds);
    }

    public async Task EnsureOverrideFitsAsync(ContainerShiftOverride containerOverride, CancellationToken cancellationToken = default)
    {
        var timeRangeShiftIds = await LoadTimeRangeShiftIdsAsync(
            containerOverride.ContainerShiftOverrideItems.Select(item => item.ShiftId), cancellationToken);
        ContainerFitCheck.EnsureOverrideFits(containerOverride, timeRangeShiftIds);
    }

    private async Task<IReadOnlySet<Guid>> LoadTimeRangeShiftIdsAsync(IEnumerable<Guid?> shiftIds, CancellationToken cancellationToken)
    {
        var ids = shiftIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return new HashSet<Guid>();
        }

        return await _shiftReader.GetTimeRangeShiftIdsAsync(ids, cancellationToken);
    }
}
