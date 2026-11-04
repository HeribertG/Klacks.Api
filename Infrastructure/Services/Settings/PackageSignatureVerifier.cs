// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Verifies the RSA-SHA256 (PKCS#1 v1.5) signature of a package downloaded from the Klacks Marketplace
/// (region package, language pack, feature plugin) against the deployment trust root
/// (Update:SignaturePublicKey, PEM) — the same vendor key and scheme the out-of-process updater uses for
/// release artifacts. Policy: once a public key is configured, a missing or invalid signature is always
/// rejected regardless of Update:RequireSignedPackages, so an on-path attacker cannot downgrade
/// verification by stripping the signature header. Without a configured public key, downloads are
/// rejected when signed packages are required (Update:RequireSignedPackages or the earlier
/// Update:RequireSignedRegionPackages) and otherwise accepted with a warning, so installations without
/// vendor keys keep working until signing is rolled out.
/// </summary>
/// <param name="options">Deployment trust configuration (public key, signature requirement)</param>
/// <param name="logger">Logger for diagnostic output</param>
using System.Security.Cryptography;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class PackageSignatureVerifier : IPackageSignatureVerifier
{
    private const string NoPublicKeyRequiredMessage =
        "Package signatures are required (Update:RequireSignedPackages) but no signature public key is configured (Update:SignaturePublicKey).";

    private const string NoPublicKeyAcceptedUnverifiedMessage =
        "No signature public key configured (Update:SignaturePublicKey) — marketplace package download accepted unverified.";

    private const string MissingSignatureWithConfiguredKeyMessage =
        "Package download carries no signature although a signature public key is configured (Update:SignaturePublicKey) — rejected to prevent a signature-stripping downgrade.";

    private const string InvalidSignatureMessage =
        "Package signature verification failed: the download does not match the configured vendor public key.";

    private readonly UpdateTrustOptions _options;
    private readonly ILogger<PackageSignatureVerifier> _logger;

    public PackageSignatureVerifier(
        IOptions<UpdateTrustOptions> options,
        ILogger<PackageSignatureVerifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public PackageSignatureVerification Verify(byte[] payload, string? signatureBase64)
    {
        if (string.IsNullOrWhiteSpace(_options.SignaturePublicKey))
        {
            if (_options.SignedPackagesRequired)
            {
                _logger.LogError("{Message}", NoPublicKeyRequiredMessage);
                return PackageSignatureVerification.Rejected(NoPublicKeyRequiredMessage);
            }

            _logger.LogWarning("{Message}", NoPublicKeyAcceptedUnverifiedMessage);
            return PackageSignatureVerification.Ok();
        }

        if (string.IsNullOrWhiteSpace(signatureBase64))
        {
            _logger.LogError("{Message}", MissingSignatureWithConfiguredKeyMessage);
            return PackageSignatureVerification.Rejected(MissingSignatureWithConfiguredKeyMessage);
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_options.SignaturePublicKey);

            var signature = Convert.FromBase64String(signatureBase64);
            var valid = rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!valid)
            {
                _logger.LogError("{Message}", InvalidSignatureMessage);
                return PackageSignatureVerification.Rejected(InvalidSignatureMessage);
            }

            return PackageSignatureVerification.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Package signature verification error.");
            return PackageSignatureVerification.Rejected($"{InvalidSignatureMessage} ({ex.Message})");
        }
    }
}
