// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRetrievalExpander
{
    Task<IReadOnlyList<AgentSkill>> ExpandAsync(
        Guid agentId,
        IReadOnlyList<AgentSkill> selectedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        int freeBudget,
        CancellationToken cancellationToken = default);
}
