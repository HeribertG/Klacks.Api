// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure reconciliation for the K20 region-setup entity-import mechanism. For each desired row: if its
/// ImportSourceKey is not yet known, it is inserted. If it is known, the CALLER has already determined
/// whether the row still matches its own last-recorded import (by recomputing a hash from the row's
/// CURRENT live values and comparing it against the row's stored ImportContentHash column) — that
/// comparison is what "unedited" means here, NOT a comparison against the newly desired content. A
/// matching row is safe to update to the new desired content; a non-matching row was edited by the
/// customer since the last import and must be left untouched.
/// </summary>

namespace Klacks.Api.Application.Services.Setup;

public static class EntityImportPlanner
{
    public static IReadOnlyList<EntityImportDecision<TValues>> Plan<TValues>(
        IReadOnlyDictionary<string, bool> existingRowUneditedBySourceKey,
        IReadOnlyList<EntityImportDesired<TValues>> desired)
    {
        var decisions = new List<EntityImportDecision<TValues>>(desired.Count);

        foreach (var item in desired)
        {
            if (!existingRowUneditedBySourceKey.TryGetValue(item.SourceKey, out var isUnedited))
            {
                decisions.Add(new EntityImportDecision<TValues>(EntityImportAction.Insert, item.SourceKey, item.ContentHash, item.Values));
                continue;
            }

            var action = isUnedited ? EntityImportAction.Update : EntityImportAction.SkipEdited;
            decisions.Add(new EntityImportDecision<TValues>(action, item.SourceKey, item.ContentHash, item.Values));
        }

        return decisions;
    }
}
