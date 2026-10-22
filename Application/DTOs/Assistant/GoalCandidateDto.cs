// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A goal candidate as the client sees it. TitleKey/RationaleKey plus RationaleParams are what the
/// frontend renders, so the proposal appears in the user's own UI language; Title and Rationale carry
/// the canonical English wording and stay in the payload only as the fallback for candidates created
/// before the goal-type catalogue existed. ChatPromptKey, when set, is the i18n key of the chat request the
/// frontend submits after approving: no plan is drafted for such a goal, because its first step runs only in
/// the chat after the user confirmed its preview.
/// </summary>

namespace Klacks.Api.Application.DTOs.Assistant;

public record GoalCandidateDto
{
    public Guid Id { get; init; }

    public string? GoalType { get; init; }

    public string? TitleKey { get; init; }

    public string? RationaleKey { get; init; }

    public IReadOnlyDictionary<string, string>? RationaleParams { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Rationale { get; init; } = string.Empty;

    public string Confidence { get; init; } = string.Empty;

    public string SignalSource { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? CreatedUtc { get; init; }

    public DateTime? DecidedUtc { get; init; }

    public Guid? PlanId { get; init; }

    public string? ChatPromptKey { get; init; }
}
