// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// UserAction values stored in klacksy_navigation_feedback. AcceptedMatch/GaveUp describe the
/// server-side matcher's own verdict on every turn; Scrolled/TargetMiss/PermissionDenied/
/// FeatureDisabled are the browser-reported outcome of a navigation it actually executed;
/// SuspectedMiss is logged by the server itself when the model navigated without a target although
/// a mid-score candidate existed. PermissionDenied and FeatureDisabled are kept apart because they
/// call for different answers: one can be granted, the other has to be installed or configured.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public static class NavigationOutcomeKinds
{
    public const string AcceptedMatch = "accepted-match";
    public const string GaveUp = "gave-up";
    public const string Scrolled = "scrolled";
    public const string TargetMiss = "target-miss";
    public const string PermissionDenied = "permission-denied";
    public const string FeatureDisabled = "feature-disabled";
    public const string SuspectedMiss = "suspected-miss";
}
