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
}
