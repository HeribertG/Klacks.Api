// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deployment-level trust root for auto-updates, bound from the "Update" configuration section
/// (appsettings/environment) — deliberately NOT in the admin-editable Settings table, so the
/// settings UI cannot redirect the manifest source or accept updates signed by a different key.
/// RequireSignedRegionPackages only matters while no SignaturePublicKey is configured: it defaults
/// to false so installations without vendor keys keep receiving region-package updates from a
/// marketplace that does not sign downloads yet, and set to true it rejects such unverifiable
/// downloads. Once a public key is configured, a missing or invalid signature is always rejected
/// regardless of this flag.
/// </summary>
namespace Klacks.Api.Application.Configuration;

public class UpdateTrustOptions
{
    public const string SectionName = "Update";

    public string ManifestBaseUrl { get; set; } = string.Empty;

    public string SignaturePublicKey { get; set; } = string.Empty;

    public bool RequireSignedRegionPackages { get; set; }
}
