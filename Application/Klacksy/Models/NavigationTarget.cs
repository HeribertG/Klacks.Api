// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Represents a single navigable in-page target resolved from data-klacksy-target attributes.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public sealed class NavigationTarget
{
    public required string TargetId { get; init; }
    public required string Route { get; init; }
    public required string LabelKey { get; init; }
    public string? Category { get; init; }
    public string? RequiredPermission { get; init; }
    public string? SourceFile { get; init; }
    public DateTime LastScannedAt { get; init; }
    public Dictionary<string, string[]> Synonyms { get; set; } = new();
    public string SynonymStatus { get; set; } = "pending";
    public bool Obsolete { get; init; }
}
