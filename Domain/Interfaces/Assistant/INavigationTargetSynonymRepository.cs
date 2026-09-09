// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for language-specific navigation target synonyms. Self-committing: every write method
/// calls SaveChangesAsync internally, there is no separate IUnitOfWork.CompleteAsync() step.
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

    /// <summary>
    /// Reconciles seed-owned rows for one (TargetId, Language) pair against the manifest keyword set,
    /// row by row. Rows whose Source is not "seed" (customer-trained or plugin-installed) are never
    /// modified or removed. A manifest keyword already present under any source (case-insensitive) is
    /// not inserted again. Keyword existence is compared case-insensitively; the underlying unique index
    /// on (TargetId, Language, Keyword) is case-sensitive, so this method never attempts to insert a row
    /// that already exists case-insensitively and therefore cannot violate that index.
    /// </summary>
    Task<NavigationTargetSynonymSyncResult> SyncSeedKeywordsForTargetLanguageAsync(string targetId, string language, IReadOnlyCollection<string> keywords, CancellationToken ct = default);
}
