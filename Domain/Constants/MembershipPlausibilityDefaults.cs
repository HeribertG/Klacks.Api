// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared constants for the membership ValidFrom plausibility check. The window bounds cap how far
/// a start date may sit in the past or future before a human confirmation is required, and the
/// override parameter carries the user's explicit confirmation back into the replayed skill call so
/// the check is skipped exactly once.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class MembershipPlausibilityDefaults
{
    public const int MaxYearsInPast = 50;

    public const int MaxYearsInFuture = 50;

    public const string ValidFromConfirmedParameter = "validFromPlausibilityConfirmed";

    public const string ConfirmedValue = "true";
}
