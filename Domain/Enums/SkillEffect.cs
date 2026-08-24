// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// The effect a skill has when it runs — orthogonal to WHO or WHAT triggers it (user turn, a prepared
/// plan, or the heartbeat). Consulting is not a separate skill type: Advise is a skill that recommends
/// an action without performing it, distinct from a Read that only reports facts. Purely descriptive
/// taxonomy — it does NOT feed SkillRiskClassifier, which stays hardcoded and reviewable on purpose.
/// </summary>
public enum SkillEffect
{
    /// <summary>Answers a "how/why does this work" question. Backed by handlerType knowledge-happen.</summary>
    Explain,

    /// <summary>Reports current facts or state without changing anything.</summary>
    Read,

    /// <summary>Recommends a concrete action without performing it. Curated, not name-derived.</summary>
    Advise,

    /// <summary>Changes state — creates, updates or deletes data.</summary>
    Mutate
}
