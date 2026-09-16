// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The decision the planning reached: this turn corrects the previous one, it routes on the composite of
/// both messages, and the previous turn's skills are off the table for it. CorrectionMessage is carried
/// verbatim rather than recovered from the composite - a correction spanning several lines cannot be
/// split back off a newline-joined composite.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GracefulCorrectionPlan(
    AssistantLastAction LastAction,
    string CorrectionMessage,
    string CompositeMessage,
    IReadOnlyList<string> ExcludedSkillNames);
