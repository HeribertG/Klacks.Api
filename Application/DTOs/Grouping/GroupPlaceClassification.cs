// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

public record GroupPlaceClassification(bool IsPlace, string? CanonicalName, string? Region, double Confidence)
{
    public static GroupPlaceClassification NotAPlace { get; } = new(false, null, null, 0.0);
}
