// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Owner rule "a container holds all of its services" for the container template and the day override: every item must
/// lie inside the container with the time it actually occupies (the planned visit time of a TimeRange shift, the own
/// time of every other item) and its travel, briefing and debriefing times.
/// </summary>
public interface IContainerFitGuard
{
    /// <summary>Throws ContainerFitRefusedException when an item of the template reaches outside of it.</summary>
    /// <param name="template">The template as it will be saved</param>
    Task EnsureTemplateFitsAsync(ContainerTemplate template, CancellationToken cancellationToken = default);

    /// <summary>Throws ContainerFitRefusedException when an item of the day override reaches outside of it.</summary>
    /// <param name="containerOverride">The day override as it will be saved</param>
    Task EnsureOverrideFitsAsync(ContainerShiftOverride containerOverride, CancellationToken cancellationToken = default);
}
