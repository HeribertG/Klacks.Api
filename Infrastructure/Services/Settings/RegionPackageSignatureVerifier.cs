// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verifies the RSA-SHA256 (PKCS#1 v1.5) signature of a downloaded region package against the
/// deployment trust root (Update:SignaturePublicKey, PEM) — the same vendor key and scheme the
/// out-of-process updater uses for release artifacts. Policy: once a public key is configured, a
/// missing or invalid signature is always rejected regardless of Update:RequireSignedRegionPackages,
/// so an on-path attacker cannot downgrade verification by stripping the signature header. Without
/// a configured public key, downloads are rejected when Update:RequireSignedRegionPackages is true
/// (unverifiable) and otherwise accepted with a warning, so installations without vendor keys keep
/// working until signing is rolled out.
/// </summary>
/// <param name="options">Deployment trust configuration (public key, signature requirement)</param>
/// <param name="logger">Logger for diagnostic output</param>
using System.Security.Cryptography;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionPackageSignatureVerifier : IRegionPackageSignatureVerifier
{
    private const string NoPublicKeyRequiredMessage =
        "Region package signatures are required (Update:RequireSignedRegionPackages) but no signature public key is configured (Update:SignaturePublicKey).";

    private const string NoPublicKeyAcceptedUnverifiedMessage =
        "No signature public key configured (Update:SignaturePublicKey) — region package download accepted unverified.";

    private const string MissingSignatureWithConfiguredKeyMessage =
        "Region package download carries no signature although a signature public key is configured (Update:SignaturePublicKey) — rejected to prevent a signature-stripping downgrade.";

    private const string InvalidSignatureMessage =
        "Region package signature verification failed: the download does not match the configured vendor public key.";

    private readonly UpdateTrustOptions _options;
    private readonly ILogger<RegionPackageSignatureVerifier> _logger;

    public RegionPackageSignatureVerifier(
        IOptions<UpdateTrustOptions> options,
        ILogger<RegionPackageSignatureVerifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public RegionPackageSignatureVerification Verify(byte[] payload, string? signatureBase64)
    {
        if (string.IsNullOrWhiteSpace(_options.SignaturePublicKey))
        {
            if (_options.RequireSignedRegionPackages)
            {
                _logger.LogError("{Message}", NoPublicKeyRequiredMessage);
                return RegionPackageSignatureVerification.Rejected(NoPublicKeyRequiredMessage);
            }

            _logger.LogWarning("{Message}", NoPublicKeyAcceptedUnverifiedMessage);
            return RegionPackageSignatureVerification.Ok();
        }

        if (string.IsNullOrWhiteSpace(signatureBase64))
        {
            _logger.LogError("{Message}", MissingSignatureWithConfiguredKeyMessage);
            return RegionPackageSignatureVerification.Rejected(MissingSignatureWithConfiguredKeyMessage);
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
                return RegionPackageSignatureVerification.Rejected(InvalidSignatureMessage);
            }

            return RegionPackageSignatureVerification.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Region package signature verification error.");
            return RegionPackageSignatureVerification.Rejected($"{InvalidSignatureMessage} ({ex.Message})");
        }
    }
}
