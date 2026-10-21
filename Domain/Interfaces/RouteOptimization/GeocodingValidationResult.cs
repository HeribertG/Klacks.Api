// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public class GeocodingValidationResult
{
    public bool Found { get; set; }
    /// <summary>
    /// True when the geocoder could not be asked at all (network error, non-success response). Found is
    /// false then too, but it means "unknown", not "this address does not exist".
    /// </summary>
    public bool ServiceUnavailable { get; set; }
    /// <summary>
    /// True when the operator switched geocoding off. Found is false then too, but the address was never checked.
    /// </summary>
    public bool Disabled { get; set; }
    public bool ExactMatch { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReturnedAddress { get; set; }
    public string? MatchType { get; set; }
    public string? State { get; set; }
    /// <summary>
    /// The geocoder's "county" (for France the département, whereas State holds the région).
    /// </summary>
    public string? County { get; set; }
}
