// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deployment-level trust root for auto-updates and marketplace packages, bound from the "Update"
/// configuration section (appsettings/environment) — deliberately NOT in the admin-editable Settings
/// table, so the settings UI cannot redirect the manifest source or accept packages signed by a
/// different key. RequireSignedPackages only matters while no SignaturePublicKey is configured: it
/// defaults to false so installations without vendor keys keep receiving region packages, language
/// packs and feature plugins from a marketplace that does not sign downloads yet, and set to true it
/// rejects such unverifiable downloads. RequireSignedRegionPackages is the earlier name of the same
/// switch and is still honoured (either flag set means required); note that it now also covers language
/// packs and feature plugins, not only region packages. Once a public key is configured, a missing or invalid signature is always
/// rejected regardless of both flags.
/// </summary>
namespace Klacks.Api.Application.Configuration;

public class UpdateTrustOptions
{
    public const string SectionName = "Update";

    public string ManifestBaseUrl { get; set; } = string.Empty;

    public string SignaturePublicKey { get; set; } = string.Empty;

    public bool RequireSignedPackages { get; set; }

    public bool RequireSignedRegionPackages { get; set; }

    public bool SignedPackagesRequired => RequireSignedPackages || RequireSignedRegionPackages;
}
