// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Well-known source markers stamped onto rows produced by the absence-recovery flow so the deferred
/// WizardRunCapture measurement can attribute a post-apply change to recovery (event churn) rather than
/// to a planner correction. This is a convention written into an existing free-text field, not a
/// database constraint — a marker match is treated as best-effort evidence, never as a hard invariant.
/// </summary>
public static class RecoveryMarkers
{
    /// <summary>
    /// Stamped into <c>WorkChange.Description</c> of every replacement the absence-recovery flow creates,
    /// so the measurement recognises a covering reassignment as an event (absence) rather than a correction.
    /// </summary>
    public const string WorkChangeSource = "recovery:absence-cover";
}
