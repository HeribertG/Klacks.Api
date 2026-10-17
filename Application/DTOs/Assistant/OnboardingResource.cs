// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Onboarding slice of the welcome payload (setup or intro tour, see Kind). Tells the frontend whether to proactively offer the
/// setup tour, whether to show the resumable progress card, and which stations are already done.
/// </summary>
public class OnboardingResource
{
    /// <summary>
    /// Which tour this slice describes: "setup" (installation-wide admin setup tour) or "intro" (per-account
    /// short intro). See <see cref="Constants.OnboardingKind"/>.
    /// </summary>
    public string Kind { get; set; } = Constants.OnboardingKind.Setup;

    /// <summary>
    /// Intro only: true while the account still holds its single release-time offer (existing account). The
    /// frontend shows that offer once and without the "later" chip.
    /// </summary>
    public bool IsLegacyOffer { get; set; }

    /// <summary>
    /// Intro only: number of explicit "later" answers so far; the second one dismisses the offer.
    /// </summary>
    public int SnoozeCount { get; set; }

    /// <summary>
    /// Intro only: when the frontend last reported the offer as visible (UTC), or null.
    /// </summary>
    public DateTime? LastOfferedAt { get; set; }

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
    /// Current lifecycle status: <see cref="Constants.OnboardingStatus"/> for setup,
    /// <see cref="Domain.Constants.IntroTourStatus"/> for intro ("pending" also when the account has no row yet).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Station ids the user has already completed. The frontend owns the full station catalog and
    /// derives progress (done / total) from it.
    /// </summary>
    public List<string> CompletedStations { get; set; } = new();
}
