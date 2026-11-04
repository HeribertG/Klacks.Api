// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Downloads a package (feature plugin ZIP, language pack bundle) from the Klacks Marketplace with a hard
/// byte cap and verifies its vendor signature before any caller sees the bytes. The response is read
/// with ResponseHeadersRead, a declared Content-Length above the cap is rejected before the body is
/// read, and the body itself is counted while it streams in, so neither a lying header nor a chunked
/// response can exceed the cap. The signature (header X-Klacks-Signature) is checked through the shared
/// <see cref="IPackageSignatureVerifier"/> (the same key policy region packages use), but over the
/// identity-bound <see cref="PackageSignatureStatement"/> of artifact type, requested name and the SHA-256 of
/// the exact response bytes, so a correctly signed download of another artifact is rejected. The whole
/// transfer, body included, runs under MarketplacePackageLimits.DownloadTimeoutSeconds (HttpClient.Timeout
/// no longer applies once the headers arrived); running out of time ends as Unavailable, while a
/// cancellation by the caller is passed on.
/// </summary>
/// <param name="signatureVerifier">Vendor signature check shared by all marketplace package downloads</param>
/// <param name="logger">Logger for diagnostic output</param>
/// <param name="downloadTimeout">Upper bound for the whole download (headers and body)</param>
/// <param name="artifactType">One of MarketplaceArtifactTypes, bound into the verified statement</param>
/// <param name="artifactName">Requested plugin name or language code, bound into the verified statement</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class MarketplacePackageDownloader
{
    private const int ReadBufferBytes = 81920;

    private readonly IPackageSignatureVerifier _signatureVerifier;
    private readonly ILogger<MarketplacePackageDownloader> _logger;
    private readonly TimeSpan _downloadTimeout;

    public MarketplacePackageDownloader(
        IPackageSignatureVerifier signatureVerifier,
        ILogger<MarketplacePackageDownloader> logger)
        : this(signatureVerifier, logger, TimeSpan.FromSeconds(MarketplacePackageLimits.DownloadTimeoutSeconds))
    {
    }

    internal MarketplacePackageDownloader(
        IPackageSignatureVerifier signatureVerifier,
        ILogger<MarketplacePackageDownloader> logger,
        TimeSpan downloadTimeout)
    {
        _signatureVerifier = signatureVerifier;
        _logger = logger;
        _downloadTimeout = downloadTimeout;
    }

    public async Task<MarketplacePackageDownload> DownloadAsync(
        HttpClient httpClient,
        string url,
        string artifactType,
        string artifactName,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_downloadTimeout);
        var downloadToken = timeoutSource.Token;

        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, downloadToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Marketplace package download '{Url}' failed with status {StatusCode}",
                    url.ForLog(),
                    response.StatusCode);
                return MarketplacePackageDownload.Failed(MarketplaceDownloadStatus.Unavailable);
            }

            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength > maxBytes)
            {
                LogTooLarge(url, maxBytes);
                return MarketplacePackageDownload.Failed(MarketplaceDownloadStatus.TooLarge);
            }

            var content = await ReadBoundedAsync(response.Content, maxBytes, downloadToken);
            if (content == null)
            {
                LogTooLarge(url, maxBytes);
                return MarketplacePackageDownload.Failed(MarketplaceDownloadStatus.TooLarge);
            }

            var signature = response.Headers.TryGetValues(HttpHeaderNames.KlacksSignature, out var values)
                ? values.FirstOrDefault()
                : null;

            var statement = PackageSignatureStatement.Build(artifactType, artifactName, content);
            var verification = _signatureVerifier.Verify(statement, signature);
            if (!verification.Accepted)
            {
                _logger.LogWarning(
                    "Marketplace package download '{Url}' rejected by the signature check: {Error}",
                    url.ForLog(),
                    verification.Error);
                return MarketplacePackageDownload.Failed(MarketplaceDownloadStatus.SignatureRejected);
            }

            return MarketplacePackageDownload.Verified(content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Marketplace package download '{Url}' did not finish within {TimeoutSeconds} seconds",
                url.ForLog(),
                _downloadTimeout.TotalSeconds);
            return MarketplacePackageDownload.Failed(MarketplaceDownloadStatus.Unavailable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Marketplace package download '{Url}' failed", url.ForLog());
            return MarketplacePackageDownload.Failed(MarketplaceDownloadStatus.Unavailable);
        }
    }

    private static async Task<byte[]?> ReadBoundedAsync(HttpContent content, long maxBytes, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[ReadBufferBytes];
        long total = 0;
        int read;

        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private void LogTooLarge(string url, long maxBytes) =>
        _logger.LogWarning(
            "Marketplace package download '{Url}' exceeds the limit of {MaxBytes} bytes and was rejected",
            url.ForLog(),
            maxBytes);
}
