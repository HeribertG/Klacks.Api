// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of a region-package signature check: either the download is accepted for import or it
/// is rejected with a reason suitable for the update status entry.
/// </summary>
/// <param name="Accepted">True when the download may be applied</param>
/// <param name="Error">Rejection reason; null when the download is accepted</param>
namespace Klacks.Api.Application.DTOs.Config;

public sealed record RegionPackageSignatureVerification(bool Accepted, string? Error)
{
    public static RegionPackageSignatureVerification Ok() => new(true, null);

    public static RegionPackageSignatureVerification Rejected(string error) => new(false, error);
}
