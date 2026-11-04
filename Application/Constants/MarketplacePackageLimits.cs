// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hard limits for packages downloaded from the Klacks Marketplace (feature plugin ZIPs and language
/// pack bundles). The Marketplace accepts plugin bundles up to 10 MB and the largest language pack is
/// about 2 MB, so the download caps leave headroom while bounding memory; the unpack limits stop a zip
/// bomb before a plugin archive is extracted. The download timeout bounds the whole transfer, body included,
/// because HttpClient.Timeout stops applying once the response headers have arrived.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class MarketplacePackageLimits
{
    public const long MaxPluginDownloadBytes = 20L * 1024 * 1024;

    public const long MaxLanguagePackDownloadBytes = 20L * 1024 * 1024;

    public const long MaxPluginUnpackedBytes = 100L * 1024 * 1024;

    public const int MaxPluginZipEntries = 2000;

    public const int MaxPluginCompressionRatio = 100;

    public const long CompressionRatioCheckThresholdBytes = 1024L * 1024;

    public const int DownloadTimeoutSeconds = 120;
}
