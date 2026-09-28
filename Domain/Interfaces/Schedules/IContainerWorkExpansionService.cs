// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Expands a container work into sub-works and sub-breaks based on its ContainerTemplate items.
/// </summary>
/// <param name="containerWork">The saved container work to expand</param>
/// <param name="date">The work date, used to determine weekday and select the correct template</param>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkExpansionService
{
    Task ExpandAsync(Work containerWork, DateOnly date);
}
