// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for language-specific navigation target synonyms.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface INavigationTargetSynonymRepository
{
    Task<IReadOnlyList<NavigationTargetSynonym>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NavigationTargetSynonym>> GetByLanguagesAsync(IEnumerable<string> languages, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<NavigationTargetSynonym> synonyms, CancellationToken ct = default);
    Task ReplaceForTargetLanguageAsync(string targetId, string language, IEnumerable<string> keywords, string source, CancellationToken ct = default);
    Task<IReadOnlyList<NavigationTargetSynonym>> GetActiveForTargetLanguageAsync(string targetId, string language, CancellationToken ct = default);
    Task<bool> HasActiveEntriesForTargetLanguageAsync(string targetId, string language, CancellationToken ct = default);
}
