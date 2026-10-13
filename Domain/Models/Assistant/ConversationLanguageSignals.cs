// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The conversation-signal lists one language contributes: what the collision table is built from. A core language
/// contributes its negations only, because the core affirmation vocabulary is not attributed to a language.
/// </summary>
/// <param name="Language">Language code as the pack directory spells it, e.g. "pl" or "zh-CN"</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConversationLanguageSignals(string Language)
{
    public IReadOnlyList<string> Affirmations { get; init; } = [];

    public IReadOnlyList<string> Negations { get; init; } = [];

    public IReadOnlyList<string> Declines { get; init; } = [];

    public IReadOnlyList<string> Cancellations { get; init; } = [];

    public IReadOnlyList<string> Corrections { get; init; } = [];

    public IReadOnlyList<string> GapIndicators { get; init; } = [];
}
