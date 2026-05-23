// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Backend mirror of the Klacks.Ui KlacksyPageKeyEntry. Populated by the
/// scanner-generated klacksy-page-keys.generated.json — never edited by hand.
/// </summary>
/// <param name="PageKey">LLM-facing identifier (lowercase, kebab-case).</param>
/// <param name="Route">Full Angular route (without entity id).</param>
/// <param name="RequiredPermission">Permissions.cs constant required; null = any user.</param>
/// <param name="HasEntityParam">true when the destination expects an entity id appended.</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record KlacksyPageKeyEntry(
    string PageKey,
    string Route,
    string? RequiredPermission,
    bool HasEntityParam);
