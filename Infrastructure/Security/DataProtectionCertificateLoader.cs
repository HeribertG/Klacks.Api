// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the X509 certificate that wraps the DataProtection key ring. The key ring itself lives in
/// the database so a single pg_dump covers it; without this certificate that dump would carry the
/// keys in clear text and the encryption of the stored secrets would be worthless.
/// </summary>
/// <param name="options">Certificate source: base64 blob (container friendly) or a file path</param>

using System.Security.Cryptography.X509Certificates;
using Klacks.Api.Application.Configuration;

namespace Klacks.Api.Infrastructure.Security;

public static class DataProtectionCertificateLoader
{
    public static X509Certificate2? Load(DataProtectionKeyRingOptions options, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(options.CertificateBase64))
        {
            return LoadFromBase64(options.CertificateBase64, options.CertificatePassword, logger);
        }

        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            return LoadFromFile(options.CertificatePath, options.CertificatePassword, logger);
        }

        return null;
    }

    private static X509Certificate2? LoadFromBase64(string base64, string? password, ILogger logger)
    {
        try
        {
            var raw = Convert.FromBase64String(base64);
            return X509CertificateLoader.LoadPkcs12(raw, password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The configured DataProtection certificate could not be read from its base64 value.");
            throw;
        }
    }

    private static X509Certificate2? LoadFromFile(string path, string? password, ILogger logger)
    {
        if (!File.Exists(path))
        {
            logger.LogError("The configured DataProtection certificate was not found at {Path}.", path);
            throw new FileNotFoundException("DataProtection certificate not found.", path);
        }

        try
        {
            return X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The configured DataProtection certificate at {Path} could not be read.", path);
            throw;
        }
    }
}
