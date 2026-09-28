// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public record GroupPlaceClassification(bool IsPlace, string? CanonicalName, string? Region, double Confidence)
{
    public static GroupPlaceClassification NotAPlace { get; } = new(false, null, null, 0.0);
}
