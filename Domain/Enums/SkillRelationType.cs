// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum SkillRelationType
{
    /// <summary>Both skills are used together. Symmetric: the order of A and B carries no meaning.</summary>
    CoRequired,

    /// <summary>B follows A in a workflow. Directed from A to B.</summary>
    Sequential,

    /// <summary>
    /// A is an action skill, B is the knowledge skill that explains it. Directed and asymmetric:
    /// A is ALWAYS the Act, B is ALWAYS the KnowHow. Carries the "why?" turn of the dialogue —
    /// Klacksy proposes an action, the user asks why, the explanation is reached over this edge,
    /// and the pending proposal survives so a following "do it" still executes the original action.
    /// Unlike the other two types this one is curated, not learned: a newly added knowledge skill
    /// has no usage history and would otherwise stay unreachable.
    /// </summary>
    ExplainedBy
}
