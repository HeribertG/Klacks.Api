// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deployment-level trust root for auto-updates, bound from the "Update" configuration section
/// (appsettings/environment) — deliberately NOT in the admin-editable Settings table, so the
/// settings UI cannot redirect the manifest source or accept updates signed by a different key.
/// </summary>
namespace Klacks.Api.Application.Configuration;

public class UpdateTrustOptions
{
    public const string SectionName = "Update";

    public string ManifestBaseUrl { get; set; } = string.Empty;

    public string SignaturePublicKey { get; set; } = string.Empty;
}
