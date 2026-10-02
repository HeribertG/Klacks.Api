// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Output-token cap for short model probes (pre-flight ping, vision capability check, tool-calling probe).
/// Thinking models share one output budget between reasoning and answer, so a cap sized for the answer
/// alone lets the reasoning consume it (gemini-3.5-flash spent 47 of 50 tokens thinking and returned no
/// text). The headroom costs nothing on models that answer in a few tokens, because generation stops there.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ModelProbeConstants
{
    public const int ThinkingHeadroomMaxTokens = 1024;
}
