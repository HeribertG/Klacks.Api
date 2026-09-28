// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class ProposedChangeStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    /// <summary>
    /// Applied by the loop itself because the routing regression gate stayed green. Written from stage G2
    /// on; the admin card lists it so an automatic change is never invisible.
    /// </summary>
    public const string AppliedAuto = "applied_auto";

    /// <summary>
    /// Withheld because applying it would have turned a previously green golden case red.
    /// </summary>
    public const string BlockedRegression = "blocked_regression";

    /// <summary>
    /// Passed the paired gate in Gate mode. The description was measured live and put back; the change waits
    /// for the export script to write it into skill-seeds.json on a review branch.
    /// </summary>
    public const string GatePassed = "gate_passed";

    /// <summary>
    /// Written into skill-seeds.json by the export script. The installation receives it with the next release,
    /// when the seed loader applies the raised seed version.
    /// </summary>
    public const string Exported = "exported";

    /// <summary>
    /// The statuses the "Klacksy learned" card shows as editable description rows: still open, applied
    /// automatically, or blocked. Approved and rejected rows are history and stay out.
    /// </summary>
    public static readonly IReadOnlyList<string> ReviewableForLearning = [Pending, AppliedAuto, BlockedRegression];
}

public static class ProposedChangeFields
{
    public const string Description = "description";

    /// <summary>
    /// A recipe whose trigger matched an utterance it should not have matched. The row names the recipe in
    /// SkillName and the utterance in ValueAfter; SkillId is empty because a recipe is not a skill. It is
    /// never applied automatically — the new trigger wording is a human decision.
    /// </summary>
    public const string RecipeTriggerNarrowing = "recipe_trigger_narrowing";
}
