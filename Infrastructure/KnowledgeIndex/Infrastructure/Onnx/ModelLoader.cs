// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Downloads ONNX model files from a URL, verifies their SHA256 hash, and caches them on disk.
/// </summary>
/// <param name="httpClient">HTTP client used for downloading model files.</param>
public sealed class ModelLoader
{
    private readonly HttpClient _httpClient;

    public ModelLoader(HttpClient httpClient) => _httpClient = httpClient;

    public async Task EnsureFileAsync(string localPath, string url, string expectedSha256, CancellationToken ct)
    {
        var verifyHash = !string.IsNullOrEmpty(expectedSha256);

        if (File.Exists(localPath) && (!verifyHash || await HashMatchesAsync(localPath, expectedSha256, ct)))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        var tempPath = localPath + ".download";
        await using (var response = await _httpClient.GetStreamAsync(url, ct))
        await using (var file = File.Create(tempPath))
        {
            await response.CopyToAsync(file, ct);
        }

        if (verifyHash && !await HashMatchesAsync(tempPath, expectedSha256, ct))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException($"SHA256 mismatch after downloading {url}");
        }

        File.Move(tempPath, localPath, overwrite: true);
    }

    private static async Task<bool> HashMatchesAsync(string path, string expected, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }
}
