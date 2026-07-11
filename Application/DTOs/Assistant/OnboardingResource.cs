// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Onboarding slice of the welcome payload. Tells the frontend whether to proactively offer the
/// setup tour, whether to show the resumable progress card, and which stations are already done.
/// </summary>
public class OnboardingResource
{
    /// <summary>
    /// True when Klacksy should proactively offer the tour now (fresh install and the user has not
    /// yet acted on the offer). Independent of LLM availability — the tour itself is frontend-scripted.
    /// The frontend still limits this to once per session.
    /// </summary>
    public bool ShouldOffer { get; set; }

    /// <summary>
    /// True when at least one enabled LLM provider with an API key exists. The frontend can use this
    /// to steer hints (e.g. pointing to LLM setup) without gating the tour offer itself.
    /// </summary>
    public bool LlmLive { get; set; }

    /// <summary>
    /// True when the resumable progress card should be visible (onboarding started or pending,
    /// not dismissed or completed).
    /// </summary>
    public bool ShowCard { get; set; }

    /// <summary>
    /// Current onboarding lifecycle status (see <see cref="Constants.OnboardingStatus"/>).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Station ids the user has already completed. The frontend owns the full station catalog and
    /// derives progress (done / total) from it.
    /// </summary>
    public List<string> CompletedStations { get; set; } = new();
}
