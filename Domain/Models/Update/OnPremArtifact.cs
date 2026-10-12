// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A self-contained native bundle (API, UI and updater) for one runtime identifier.
/// </summary>
/// <param name="Rid">.NET runtime identifier the bundle was published for, e.g. win-x64</param>
/// <param name="BundleUrl">Download URL of the bundle archive</param>
/// <param name="Sha256">Hex SHA-256 of the bundle archive</param>
/// <param name="Signature">RSA-SHA256 signature over "{version}\n{bundleUrl}\n{sha256}"</param>

namespace Klacks.Api.Domain.Models.Update;

public class OnPremArtifact
{
    public string Rid { get; set; } = string.Empty;

    public string BundleUrl { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;
}
