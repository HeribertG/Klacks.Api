// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One selectable goal type in the reflection catalogue, bound to exactly one proactive trigger kind.
/// Splits the three audiences a goal text serves: the reflection LLM (SignalDescription), the user
/// interface (TitleKey/RationaleKey, rendered by the frontend in the user's UI language) and the
/// planning agent plus audit log (PlannerTitle/PlannerRationaleFormat, canonical English). No field
/// ever carries a technical identifier, so no trigger kind or column name can reach a user-facing text.
/// </summary>
/// <param name="TriggerKind">Proactive trigger kind this goal type is derived from; see AgentTriggerKinds.</param>
/// <param name="TitleKey">Frontend i18n key for the goal title shown to the user.</param>
/// <param name="RationaleKey">Frontend i18n key for the rationale, interpolated with the count and days parameters.</param>
/// <param name="SignalDescription">Plain-language English description handed to the reflection LLM instead of the trigger kind.</param>
/// <param name="PlannerTitle">Canonical English goal title used for plan drafting and the audit log.</param>
/// <param name="PlannerRationaleFormat">Canonical English rationale, formatted with the occurrence count and the lookback window in days.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoalTypeDefinition(
    string TriggerKind,
    string TitleKey,
    string RationaleKey,
    string SignalDescription,
    string PlannerTitle,
    string PlannerRationaleFormat);
