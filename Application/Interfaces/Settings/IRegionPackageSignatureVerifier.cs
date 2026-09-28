// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Verifies the vendor signature of a downloaded marketplace region package before it is applied.
/// </summary>
using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface IRegionPackageSignatureVerifier
{
    RegionPackageSignatureVerification Verify(byte[] payload, string? signatureBase64);
}
