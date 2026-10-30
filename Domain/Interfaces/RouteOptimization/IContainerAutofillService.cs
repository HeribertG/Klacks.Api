// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Service for automatic filling of container templates with matching shifts.
/// Solves an Orienteering Problem: select a subset of shifts within the time budget and optimize the route.
/// </summary>
/// <param name="request">Contains container ID, time window, base addresses and transport mode</param>

using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IContainerAutofillService
{
    Task<ContainerAutofillResult> AutofillAsync(ContainerAutofillRequest request);

    /// <summary>
    /// Dry run of the same planning on tasks the caller already loaded: nothing is stored (no coordinates) and no
    /// turn-by-turn directions are requested. The ant colony step is randomized, so with more than two stops the route of
    /// a later real run can differ.
    /// </summary>
    /// <param name="request">Tasks, bases, time frame and transport mode</param>
    Task<ContainerAutofillPreview> PreviewAsync(ContainerAutofillPreviewRequest request);
}
