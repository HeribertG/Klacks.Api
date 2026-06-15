// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSequenceSuggester
{
    Task<string?> SuggestNextAsync(
        Guid agentId,
        string justExecutedSkill,
        IReadOnlyCollection<string> alreadySuggested,
        CancellationToken cancellationToken = default);
}
