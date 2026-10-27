// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stable persisted names of <see cref="EvalMissKind"/>. The strings are what eval_run_items.miss_kind and the dimension
/// counts store, so they never follow a rename of an enum member.
/// </summary>
/// <param name="kind">The miss kind to name</param>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class EvalMissKindNames
{
    public const string Correct = "correct";
    public const string ReachedViaFollowUp = "reached_via_follow_up";
    public const string LookupFirst = "lookup_first";
    public const string SurfaceSearch = "surface_search";
    public const string WrongWrite = "wrong_write";
    public const string WrongRead = "wrong_read";
    public const string NoToolText = "no_tool_text";
    public const string NotInToolsetTruncated = "not_in_toolset_truncated";
    public const string NotInToolsetNotRetrieved = "not_in_toolset_not_retrieved";
    public const string ToolNotRegistered = "tool_not_registered";
    public const string NotApplicable = "not_applicable";

    public static string Of(EvalMissKind kind) => kind switch
    {
        EvalMissKind.Correct => Correct,
        EvalMissKind.ReachedViaFollowUp => ReachedViaFollowUp,
        EvalMissKind.LookupFirst => LookupFirst,
        EvalMissKind.SurfaceSearch => SurfaceSearch,
        EvalMissKind.WrongWrite => WrongWrite,
        EvalMissKind.WrongRead => WrongRead,
        EvalMissKind.NoToolText => NoToolText,
        EvalMissKind.NotInToolsetTruncated => NotInToolsetTruncated,
        EvalMissKind.NotInToolsetNotRetrieved => NotInToolsetNotRetrieved,
        EvalMissKind.ToolNotRegistered => ToolNotRegistered,
        EvalMissKind.NotApplicable => NotApplicable,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
