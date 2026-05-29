// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Language-specific trigger keyword for a navigation target.
/// </summary>
/// <param name="TargetId">The identifier of the navigation target (matches NavigationTarget.TargetId in the JSON manifest)</param>
/// <param name="Language">ISO language code (e.g. "de", "en", "fr")</param>
/// <param name="Keyword">The trigger keyword in the respective language</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class NavigationTargetSynonym : BaseEntity
{
    public string TargetId { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string? Source { get; set; }
}
