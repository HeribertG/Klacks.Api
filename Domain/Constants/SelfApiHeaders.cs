// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class SelfApiHeaders
{
    /// <summary>Names the skill that caused a write, so the request log attributes it.</summary>
    public const string SkillName = "X-Klacksy-Skill";

    /// <summary>
    /// Ties the writes of one conversation together in the log — it carries the conversation id, not a
    /// per-turn value, so every write of the same chat shares it. Background paths without a
    /// conversation fall back to a fresh id per call.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";
}
