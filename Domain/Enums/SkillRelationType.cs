// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    ExplainedBy,

    /// <summary>
    /// A is an Advise-effect skill, B is the Act skill it recommends. Directed: A always evaluates or
    /// proposes, B always performs. Curated, not learned — same reasoning as ExplainedBy: a freshly
    /// curated Advise skill has no usage history of its own yet. Appended last: the underlying column
    /// stores the enum ordinal and a unique index keys off it, so existing rows must not shift.
    /// </summary>
    AdvisesFor,

    /// <summary>
    /// A's description names B as a cross-reference ("see also"), including a deliberate delimitation
    /// ("not this one — that one"). Curated documentation metadata, NOT an experience claim: it exists
    /// so a name-drop inside a skill description is backed by a graph edge. Since 2026-11-14 it takes part in
    /// the skill-graph association like every active relation (SkillRetrievalExpander), weighted at a fifth
    /// against the candidate's own retrieval score; the older rule that kept it retrieval-neutral (W3.2) was
    /// made for a confidence-only ranking in which its 1.0 won every slot. Appended last:
    /// the underlying column stores the enum ordinal and a unique index keys off it, so existing rows
    /// must not shift.
    /// </summary>
    SeeAlso
}
