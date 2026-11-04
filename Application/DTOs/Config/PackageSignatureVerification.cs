// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of a marketplace package signature check: either the download is accepted for import or
/// it is rejected with a reason suitable for a status entry or a log line.
/// </summary>
/// <param name="Accepted">True when the download may be applied</param>
/// <param name="Error">Rejection reason; null when the download is accepted</param>
namespace Klacks.Api.Application.DTOs.Config;

public sealed record PackageSignatureVerification(bool Accepted, string? Error)
{
    public static PackageSignatureVerification Ok() => new(true, null);

    public static PackageSignatureVerification Rejected(string error) => new(false, error);
}
