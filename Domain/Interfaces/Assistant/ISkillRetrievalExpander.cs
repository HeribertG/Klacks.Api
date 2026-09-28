// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
