// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Entity storing user feedback on Klacksy in-page navigation utterances and routing outcomes.
/// </summary>

namespace Klacks.Api.Domain.Models.Klacksy;

using Klacks.Api.Domain.Common;

public class KlacksyNavigationFeedback : BaseEntity
{
    public string Utterance { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string? MatchedTargetId { get; set; }
    public double? MatchedScore { get; set; }
    public string UserAction { get; set; } = string.Empty;
    public string? ActualRoute { get; set; }
    public DateTime Timestamp { get; set; }
}
