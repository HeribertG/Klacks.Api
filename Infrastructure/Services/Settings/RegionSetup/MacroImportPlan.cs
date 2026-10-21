// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Planned macro import: the per-row decisions plus the current function holders that must be demoted
/// to Custom (and flushed) before the new holders are inserted.
/// </summary>
/// <param name="Rows">Insert/update/skip decisions of the imported macros</param>
/// <param name="Demotions">Tracked holder macros to demote first</param>

using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record MacroImportPlan(
    EntityImportFamilyPlan<MacroImportValues, Macro> Rows,
    IReadOnlyList<Macro> Demotions)
{
    public static MacroImportPlan Empty { get; } = new(EntityImportFamilyPlan<MacroImportValues, Macro>.Empty, []);
}
