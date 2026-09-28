// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the stored index row of a skill and confirms that its text starts with the prefix the synchronizer
/// builds from the given description. No row, or another description, is "not indexed".
/// </summary>
/// <param name="repository">Reads knowledge index rows by (kind, source id)</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Application.Services;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillIndexStateVerifier : ISkillIndexStateVerifier
{
    private readonly IKnowledgeIndexRepository _repository;

    public SkillIndexStateVerifier(IKnowledgeIndexRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsIndexedAsync(
        string skillName, string description, CancellationToken cancellationToken = default)
    {
        var entries = await _repository.GetByKeysAsync([(KnowledgeEntryKind.Skill, skillName)], cancellationToken);
        var expectedPrefix = SkillEmbeddingTextPrefix.Build(skillName, description);

        return entries.Any(entry => entry.Text.StartsWith(expectedPrefix, StringComparison.Ordinal));
    }
}
