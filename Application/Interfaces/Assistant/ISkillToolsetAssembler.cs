// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface ISkillToolsetAssembler
{
    Task<SkillToolsetResult> AssembleAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? conversationId,
        string? currentRoute,
        string userId,
        string? language,
        int maxToolsForProvider = KnowledgeIndexConstants.MaxToolsForProvider,
        bool applyLearnedPhraseGuarantee = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Full-arity overload of the call above: every parameter is required, so a caller that omits the
    /// new collections binds the short overload instead of silently shifting an argument into them.
    /// That is the lesson of the 2026-09-14 overload trap - an optional parameter appended to an
    /// existing signature rebinds positional arguments without failing to compile.
    /// </summary>
    /// <param name="excludedSkillNames">
    /// Skills this turn must not offer, i.e. the ones the corrected turn already called. Always-on
    /// skills and confirm_pending_action are exempt: the first are in every toolset by definition, the
    /// second is the user's only way to redeem a held action.
    /// </param>
    /// <param name="pinnedSkillNames">
    /// Skills that must be in the toolset regardless of retrieval, i.e. the two options a clarification
    /// question offered on the previous turn. An exclusion beats a pin.
    /// </param>
    /// <param name="previousTurnSkillNames">
    /// Names of the skills the last turn that made tool calls executed (while that record lives), or null
    /// when the caller has none. A KnowHow (Explain) or Advise skill among them keeps the Act skill it leads
    /// to in this toolset, because the answer to an interview carries no keyword of that skill. Ordered, most
    /// recent last. Required, so
    /// a caller that forgets it fails at compile time instead of silently passing nothing.
    /// </param>
    /// <param name="continuationSkillNames">
    /// Skills the IMMEDIATELY preceding turn's successful calls ran (a confirmed held action named by the skill it
    /// replayed), most recent last, or null when that record was superseded, the turn is a correction, or there is
    /// none. The last Mutate skill among them stays in this toolset (same-skill continuation), ranked below every
    /// other guarantee at truncation.
    /// </param>
    Task<SkillToolsetResult> AssembleAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? conversationId,
        string? currentRoute,
        string userId,
        string? language,
        int maxToolsForProvider,
        bool applyLearnedPhraseGuarantee,
        IReadOnlyCollection<string>? excludedSkillNames,
        IReadOnlyCollection<string>? pinnedSkillNames,
        IReadOnlyList<string>? previousTurnSkillNames,
        IReadOnlyList<string>? continuationSkillNames,
        CancellationToken cancellationToken);
}
