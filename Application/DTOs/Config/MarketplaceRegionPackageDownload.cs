// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Payload of a marketplace region-package download: the raw response bytes (the exact bytes the
/// marketplace signed), the decoded profile JSON and the optional Base64 signature taken from the
/// X-Klacks-Signature response header (null when the marketplace does not sign downloads yet).
/// </summary>
/// <param name="Content">Exact response body bytes used for signature verification</param>
/// <param name="ProfileJson">Profile JSON decoded from the response body</param>
/// <param name="Signature">Base64 RSA-SHA256 signature over the response body, or null when absent</param>
namespace Klacks.Api.Application.DTOs.Config;

public sealed record MarketplaceRegionPackageDownload(byte[] Content, string ProfileJson, string? Signature);
