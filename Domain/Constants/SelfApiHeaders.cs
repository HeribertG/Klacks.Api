// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class SelfApiHeaders
{
    /// <summary>Names the skill that caused a write, so the request log attributes it.</summary>
    public const string SkillName = "X-Klacksy-Skill";

    /// <summary>Ties the writes of one assistant turn together in the log.</summary>
    public const string CorrelationId = "X-Correlation-Id";
}
