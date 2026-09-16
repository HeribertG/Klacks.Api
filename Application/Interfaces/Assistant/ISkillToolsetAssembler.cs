// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    /// two new collections binds the short overload instead of silently shifting an argument into them.
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
        CancellationToken cancellationToken);
}
