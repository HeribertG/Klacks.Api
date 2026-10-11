// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single registration point for the global MVC action filters: the admin setup gate and the scenario token access
/// check that every controller request passes.
/// </summary>

using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Extensions;

public static class FilterCollectionExtensions
{
    /// <param name="filters">The global filter collection AddControllers hands to its configuration callback</param>
    public static FilterCollection AddKlacksGlobalFilters(this FilterCollection filters)
    {
        filters.Add<AdminSetupGateFilter>();
        filters.Add<ScenarioTokenAccessFilter>();

        return filters;
    }
}
