// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Domain-facing view of a single in-page navigation target, scoped to one route.
/// Excludes fields skills do not consume (Route, LabelKey, Category) — see
/// INavigationTargetCatalog for the route-scoping contract. RequiredPermission is carried
/// because the LLM path must refuse a target the caller may not reach, using the same
/// Permissions helper as the chat fast-path (NavigationTargetMatcher).
/// </summary>
/// <param name="TargetId">Stable identifier the LLM must pass as navigate_to's 'target' parameter.</param>
/// <param name="Synonyms">Known phrases per locale that resolve to this target (DB-sourced, may be empty).</param>
/// <param name="RequiredPermission">Roles/Permissions constant (comma-separated list = all required) the caller must hold; null = any logged-in user.</param>
/// <param name="RequiredFeature">Feature the installation must provide for this target to exist, inherited from the page-key of its route; null = it exists everywhere.</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record NavigationTargetEntry(
    string TargetId,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Synonyms,
    string? RequiredPermission = null,
    string? RequiredFeature = null);
