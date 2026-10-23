// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Auto-update policy a customer installation starts with, bound from the "Update" configuration section.
/// Only the customer installers set these keys (deploy/onprem/docker-compose.yml, the native klacks.native.json
/// written by Install-Klacks.ps1); the vendor's own production stack, development and test runs leave them unset,
/// so they never start updating themselves. The values are seeded into the admin-editable Settings table once, and
/// only for a setting that does not exist yet — after that the admin UI is authoritative. Kept as strings so a
/// mistyped value is reported and skipped instead of failing the options binding at startup.
/// </summary>
namespace Klacks.Api.Application.Configuration;

public class UpdateInitialPolicyOptions
{
    public const string SectionName = "Update";

    public string? InitialAutoEnabled { get; set; }

    public string? InitialNotifyOnly { get; set; }
}
