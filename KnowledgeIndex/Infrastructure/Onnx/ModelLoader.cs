// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Downloads ONNX model files from a URL, verifies their SHA256 hash, and caches them on disk.
/// A successful verification is recorded next to the file so that an unchanged cached model is not
/// re-hashed on every process start: the models total ~931 MB, and hashing them sat in the latency of
/// the first chat request because both providers initialize lazily.
/// </summary>
/// <param name="httpClient">HTTP client used for downloading model files.</param>
public sealed class ModelLoader
{
    private const string VerificationMarkerSuffix = ".verified";

    private readonly HttpClient _httpClient;

    public ModelLoader(HttpClient httpClient) => _httpClient = httpClient;

    public async Task EnsureFileAsync(string localPath, string url, string expectedSha256, CancellationToken ct)
    {
        var verifyHash = !string.IsNullOrEmpty(expectedSha256);

        if (File.Exists(localPath)
            && (!verifyHash || HasValidMarker(localPath, expectedSha256) || await VerifyAndMarkAsync(localPath, expectedSha256, ct)))
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

        if (verifyHash)
        {
            WriteMarker(localPath, expectedSha256);
        }
    }

    private static async Task<bool> HashMatchesAsync(string path, string expected, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }

    // Hashes the cached file once and records the result, so later starts take the marker path.
    // Covers models downloaded before this marker existed.
    private static async Task<bool> VerifyAndMarkAsync(string localPath, string expected, CancellationToken ct)
    {
        if (!await HashMatchesAsync(localPath, expected, ct))
        {
            return false;
        }

        WriteMarker(localPath, expected);
        return true;
    }

    // The marker binds the verified hash to the file's identity (length + last write time). Any change
    // to the file, or a change to the expected hash after a model switch, invalidates it and forces a
    // real re-hash. It trades integrity checking against a writer who can modify the cache directory -
    // who could equally replace the assemblies loading from it, so this does not widen the trust
    // boundary. It does NOT weaken the download path: a fresh download is always hashed in full.
    private static bool HasValidMarker(string localPath, string expected)
    {
        var markerPath = localPath + VerificationMarkerSuffix;

        if (!File.Exists(markerPath))
        {
            return false;
        }

        try
        {
            return string.Equals(File.ReadAllText(markerPath), BuildMarker(localPath, expected), StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void WriteMarker(string localPath, string expected)
    {
        try
        {
            File.WriteAllText(localPath + VerificationMarkerSuffix, BuildMarker(localPath, expected));
        }
        catch (IOException)
        {
            // A read-only or full cache directory only costs the re-hash on the next start.
        }
    }

    private static string BuildMarker(string localPath, string expected)
    {
        var info = new FileInfo(localPath);
        return $"{expected.ToUpperInvariant()}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
    }
}
