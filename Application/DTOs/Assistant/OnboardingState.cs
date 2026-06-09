// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Persisted shape of the ONBOARDING_STATE setting value (serialized as JSON).
/// A fresh seed may store the bare status string instead of JSON; the reader tolerates both.
/// </summary>
public class OnboardingState
{
    public string Status { get; set; } = OnboardingStatus.Pending;

    public List<string> CompletedStations { get; set; } = new();
}
