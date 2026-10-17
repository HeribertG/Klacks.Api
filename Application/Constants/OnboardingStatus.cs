// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lifecycle states of the Klacksy first-run setup tour, stored in the ONBOARDING_STATE setting.
/// A fresh install seeds <see cref="Pending"/>; the user's choices move it forward (or end it).
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class OnboardingStatus
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Snoozed = "snoozed";
    public const string Dismissed = "dismissed";
    public const string Completed = "completed";

    /// <summary>
    /// True while the setup tour is still open for the admin (not yet started, running or postponed); only then does
    /// the welcome payload carry the setup slice instead of the intro slice.
    /// </summary>
    public static bool IsOpen(string? status) => status is Pending or InProgress or Snoozed;

    /// <summary>
    /// True for the two final setup states; reaching one closes the admin's own intro tour.
    /// </summary>
    public static bool IsResolved(string? status) => status is Completed or Dismissed;
}
