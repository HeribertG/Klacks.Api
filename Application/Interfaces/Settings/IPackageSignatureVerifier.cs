// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Verifies the vendor signature of a package downloaded from the Klacks Marketplace (region package,
/// language pack or feature plugin) before it is parsed, extracted or applied.
/// </summary>
using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface IPackageSignatureVerifier
{
    PackageSignatureVerification Verify(byte[] payload, string? signatureBase64);
}
