// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Thresholds and keys of the explanation-depth resolution: the per-user override memory, the usage heuristic that
/// promotes a user to advanced, the skill that writes the override, and the cache lifetime of a resolved profile.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ExplainDepthDefaults
{
    public const string OverrideMemoryKey = "explain.depth.override";

    public const string SetSkillName = "set_explain_depth";
    public const string SetSkillParameterName = "depth";

    public const string OverrideValueBeginner = "beginner";
    public const string OverrideValueAdvanced = "advanced";
    public const string OverrideValueAuto = "auto";

    public const int MinOwnWorks = 100;
    public const int MinActiveSpanDays = 56;
    public const int MinSuccessfulSkillWrites = 30;

    public const int CacheMinutes = 5;

    /// <summary>
    /// Cache lifetime of an advanced level estimated by the heuristic. Usage signals accumulate, so this estimate
    /// practically never flips back; an override written by the user invalidates the entry right away.
    /// </summary>
    public const int AdvancedEstimateCacheHours = 24;
    public const string CacheKeyPrefix = "explain-depth:";

    /// <summary>
    /// Importance of a persisted override memory. AgentMemoryRepository exempts the override key from the importance
    /// decay and the low-value cleanup, so the value stays as written and the override never expires on its own.
    /// </summary>
    public const int OverrideImportance = 8;
}
