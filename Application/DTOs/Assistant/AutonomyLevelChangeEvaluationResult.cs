// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// On-demand evaluation of a proposed autonomy level change: which skill risk classes flip from
/// held-for-confirmation to run-directly (or back) on the chat gate and the plan-step gate, and how
/// many currently registered skills fall into each class. Used by the
/// evaluate_autonomy_level_change skill so Klacksy can explain the concrete effect of a level change
/// before the user confirms it — the change itself stays a manual, always-confirmed step
/// (set_autonomy_level is Sensitive and asks at every level, see SkillRiskClassifier).
/// </summary>
/// <param name="CurrentLevel">The user's current autonomy level (0-3)</param>
/// <param name="CurrentLevelName">Name of the current level</param>
/// <param name="TargetLevel">The proposed autonomy level (0-3)</param>
/// <param name="TargetLevelName">Name of the proposed level</param>
/// <param name="IsNoOp">True when target equals current — nothing would change</param>
/// <param name="IsDowngrade">True when the target level is lower than the current one</param>
/// <param name="Impacts">Per risk class: skill count and confirmation behavior at current vs. target level</param>
/// <param name="SkillsNewlyUnconfirmedInChat">Sum of SkillCount over classes whose chat confirmation requirement turns off at the target level</param>
/// <param name="SkillsNewlyConfirmedInChat">Sum of SkillCount over classes whose chat confirmation requirement turns on at the target level</param>
/// <param name="Recommendation">Conservative, factual summary of the effect — never auto-acts</param>
public sealed record AutonomyLevelChangeEvaluationResult(
    int CurrentLevel,
    string CurrentLevelName,
    int TargetLevel,
    string TargetLevelName,
    bool IsNoOp,
    bool IsDowngrade,
    IReadOnlyList<AutonomyRiskClassImpact> Impacts,
    int SkillsNewlyUnconfirmedInChat,
    int SkillsNewlyConfirmedInChat,
    string Recommendation);
