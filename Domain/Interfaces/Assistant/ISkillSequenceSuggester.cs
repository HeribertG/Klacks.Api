// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSequenceSuggester
{
    Task<string?> SuggestNextAsync(
        Guid agentId,
        string justExecutedSkill,
        IReadOnlyCollection<string> alreadySuggested,
        CancellationToken cancellationToken = default);
}
