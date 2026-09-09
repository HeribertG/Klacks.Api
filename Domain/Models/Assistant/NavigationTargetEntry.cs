// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Domain-facing view of a single in-page navigation target, scoped to one route.
/// Deliberately excludes fields skills do not consume (Route, LabelKey, Category, permissions) —
/// see INavigationTargetCatalog for the route-scoping contract.
/// </summary>
/// <param name="TargetId">Stable identifier the LLM must pass as navigate_to's 'target' parameter.</param>
/// <param name="Synonyms">Known phrases per locale that resolve to this target (DB-sourced, may be empty).</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record NavigationTargetEntry(
    string TargetId,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Synonyms);
