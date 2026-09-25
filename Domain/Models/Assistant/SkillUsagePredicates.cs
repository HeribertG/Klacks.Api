// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Linq.Expressions;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// The one definition of which usage rows carry a verdict about a skill call. A row is decided when it is no
/// UiAction (no status) or the browser reported its outcome (Completed, Failed), and it was not cut short by a
/// stop (failure kind Cancelled). A UiAction that is still Dispatched has no outcome yet, and a Cancelled one
/// never ran, so neither may count as a call, a success or a failure. Stated positively on purpose: a status
/// added later is then left out until somebody decides it, instead of silently counting as a failure in every
/// reader that used to exclude "Dispatched" by name.
/// </summary>
public static class SkillUsagePredicates
{
    /// <summary>The predicate for queries the database evaluates.</summary>
    public static readonly Expression<Func<SkillUsageRecord, bool>> HasVerdict = row =>
        (row.UiActionStatus == null
            || row.UiActionStatus == UiActionStatus.Completed
            || row.UiActionStatus == UiActionStatus.Failed)
        && row.FailureKind != SkillFailureKind.Cancelled;

    private static readonly Func<SkillUsageRecord, bool> HasVerdictCompiled = HasVerdict.Compile();

    /// <summary>The same predicate for rows already in memory.</summary>
    /// <param name="row">The usage row to test</param>
    public static bool RowHasVerdict(SkillUsageRecord row) => HasVerdictCompiled(row);
}
