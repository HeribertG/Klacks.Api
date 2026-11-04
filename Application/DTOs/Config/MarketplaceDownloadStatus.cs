// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Config;

public enum MarketplaceDownloadStatus
{
    Succeeded,
    Unavailable,
    TooLarge,
    SignatureRejected
}
