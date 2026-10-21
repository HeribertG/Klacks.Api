// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Planned insert/update/skip decisions of one entity-import family together with the existing rows the
/// Update decisions write to, computed before the import transaction opens.
/// </summary>
/// <param name="Decisions">One decision per desired row of the profile</param>
/// <param name="ExistingBySourceKey">Existing tracked rows keyed by ImportSourceKey</param>

using Klacks.Api.Application.Services.Setup;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record EntityImportFamilyPlan<TValues, TEntity>(
    IReadOnlyList<EntityImportDecision<TValues>> Decisions,
    IReadOnlyDictionary<string, TEntity> ExistingBySourceKey)
{
    public static EntityImportFamilyPlan<TValues, TEntity> Empty { get; } = new([], new Dictionary<string, TEntity>());

    public int Count => Decisions.Count;

    public bool HasWrites => Decisions.Any(d => d.Action != EntityImportAction.SkipEdited);

    public int WriteCount => Decisions.Count(d => d.Action != EntityImportAction.SkipEdited);
}
