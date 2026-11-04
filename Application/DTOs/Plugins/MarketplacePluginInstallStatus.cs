// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Plugins;

public enum MarketplacePluginInstallStatus
{
    Installed,
    InvalidName,
    Unavailable,
    TooLarge,
    SignatureRejected,
    InvalidArchive,
    InstallFailed,
    ManifestMismatch
}
