// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The autofill service is a domain service without a caller, so its picks are narrowed to the task shifts the caller may
/// see before anything is written, together with the route through them; an unrestricted caller keeps every pick and the
/// route as optimized.
/// </summary>
/// <param name="shiftVisibilityGuard">Drops the picks a restricted caller may not see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerAutofillVisibilityFilter
{
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public ContainerAutofillVisibilityFilter(IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    /// <param name="autofillResult">Raw autofill result</param>
    public async Task<ContainerAutofillResult> KeepVisibleSelectionAsync(
        ContainerAutofillResult autofillResult, CancellationToken cancellationToken)
    {
        if (autofillResult.SelectedShiftIds.Count == 0)
        {
            return autofillResult;
        }

        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(autofillResult.SelectedShiftIds, cancellationToken);
        if (visibleIds.Count == autofillResult.SelectedShiftIds.Distinct().Count())
        {
            return autofillResult;
        }

        return ContainerAutofillRouteFilter.KeepShifts(autofillResult, visibleIds);
    }
}
