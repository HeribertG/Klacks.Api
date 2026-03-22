// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for language-specific skill synonyms.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSynonymRepository
{
    Task<IReadOnlyList<SkillSynonym>> GetByLanguageAsync(string language, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<SkillSynonym> synonyms, CancellationToken ct = default);
    Task DeleteAllAsync(CancellationToken ct = default);
}
