// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum SkillLearningMode
{
    /// <summary>
    /// Only collects learning cases and clusters; no proposal is generated, no LLM call is made, no live
    /// description is ever mutated. The default for every installation.
    /// </summary>
    Collect,

    /// <summary>
    /// Gate mutates the live catalogue for minutes per proposal and is only for installations without real
    /// users; it measures only when explicitly triggered.
    /// </summary>
    Gate,

    /// <summary>
    /// A change that passes the gate stays live, and the phrase and recipe learners keep writing live; used
    /// in tests only.
    /// </summary>
    AutoApply
}
