// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of a size-capped, signature-verified package download from the Klacks Marketplace. Content
/// holds the exact response bytes and is only filled when the download succeeded and its signature
/// was accepted; every other status carries an empty array.
/// </summary>
/// <param name="Status">Outcome of the download and the signature check</param>
/// <param name="Content">Verified response bytes; empty unless Status is Succeeded</param>
namespace Klacks.Api.Application.DTOs.Config;

public sealed record MarketplacePackageDownload(MarketplaceDownloadStatus Status, byte[] Content)
{
    public bool Succeeded => Status == MarketplaceDownloadStatus.Succeeded;

    public static MarketplacePackageDownload Verified(byte[] content) => new(MarketplaceDownloadStatus.Succeeded, content);

    public static MarketplacePackageDownload Failed(MarketplaceDownloadStatus status) => new(status, []);
}
