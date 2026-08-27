// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// File-system-backed object storage for ERP order import drop points. Maps slash-separated
/// object keys to files below the configured root directory. Uploads write to a temporary
/// file first and are published via an atomic rename. Key listing (used by the import runner)
/// skips temporary files and files still inside the write-stability window, so half-written
/// files dropped into the directory by external processes are never imported; metadata listing
/// (used by the file explorer) skips only temporary files so fresh uploads are visible at once.
/// </summary>
using System.Globalization;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Imports;

public class FileSystemObjectStorageService : IObjectStorageService
{
    private const string UploadTempSuffix = ".uploading";
    private const string DefaultRootDirectoryName = "ErpImport";
    private const string HealthCheckMarkerFileName = ".klacksy-health-check";
    private const char KeySeparator = '/';
    private static readonly TimeSpan WriteStabilityWindow = TimeSpan.FromSeconds(10);

    private readonly string _rootPath;

    public FileSystemObjectStorageService(IOptions<ErpObjectStorageOptions> options)
    {
        _rootPath = ResolveRootPath(options.Value.RootPath);
    }

    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> keys = EnumerateFiles(prefix, onlyStable: true)
            .Select(file => file.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(keys);
    }

    public Task<IReadOnlyList<StorageObjectMetadata>> ListWithMetadataAsync(string prefix, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StorageObjectMetadata> objects = EnumerateFiles(prefix, onlyStable: false)
            .Select(file =>
            {
                var info = new FileInfo(file.Path);
                return new StorageObjectMetadata(file.Key, info.Length, info.LastWriteTimeUtc);
            })
            .OrderBy(storageObject => storageObject.Key, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(objects);
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(File.OpenRead(ToSafePath(key)));
    }

    public Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        var sourcePath = ToSafePath(sourceKey);
        var destinationPath = ToSafePath(destinationKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Move(sourcePath, destinationPath, overwrite: true);
        return Task.CompletedTask;
    }

    public Task<bool> TryClaimAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        var sourcePath = ToSafePath(sourceKey);
        var destinationPath = ToSafePath(destinationKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        try
        {
            File.Move(sourcePath, destinationPath, overwrite: false);
            return Task.FromResult(true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        File.Delete(ToSafePath(key));
        return Task.CompletedTask;
    }

    public async Task UploadAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var destinationPath = ToSafePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        var tempPath = destinationPath + UploadTempSuffix;
        await using (var file = File.Create(tempPath))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(tempPath, destinationPath, overwrite: true);
    }

    public Task<ObjectStorageHealthResult> CheckHealthAsync(
        IReadOnlyList<string> requiredPrefixes,
        CancellationToken cancellationToken = default)
    {
        var rootDirectoryExisted = Directory.Exists(_rootPath);
        var rootDirectoryReady = EnsureRootDirectory();
        var (isWritable, writeError) = rootDirectoryReady
            ? TestWriteAccess()
            : (false, "Storage root directory could not be created or reached.");

        var prefixResults = requiredPrefixes
            .Select(prefix => EnsurePrefixDirectory(prefix, isWritable))
            .ToList();

        return Task.FromResult(new ObjectStorageHealthResult(
            _rootPath, rootDirectoryExisted, rootDirectoryReady, isWritable, writeError, prefixResults));
    }

    private bool EnsureRootDirectory()
    {
        try
        {
            Directory.CreateDirectory(_rootPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private (bool IsWritable, string? Error) TestWriteAccess()
    {
        try
        {
            var markerPath = Path.Combine(_rootPath, HealthCheckMarkerFileName);
            File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            File.Delete(markerPath);
            return (true, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (false, exception.Message);
        }
    }

    private ObjectStoragePrefixHealth EnsurePrefixDirectory(string prefix, bool rootIsWritable)
    {
        if (!rootIsWritable)
        {
            return new ObjectStoragePrefixHealth(prefix, false, "Storage root is not writable.");
        }

        try
        {
            Directory.CreateDirectory(ToSafePath(prefix));
            return new ObjectStoragePrefixHealth(prefix, true, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new ObjectStoragePrefixHealth(prefix, false, exception.Message);
        }
    }

    private IEnumerable<(string Path, string Key)> EnumerateFiles(string prefix, bool onlyStable)
    {
        if (!Directory.Exists(_rootPath))
        {
            yield break;
        }

        var cutoff = DateTime.UtcNow - WriteStabilityWindow;
        foreach (var path in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
        {
            if (path.EndsWith(UploadTempSuffix, StringComparison.Ordinal) || (onlyStable && File.GetLastWriteTimeUtc(path) > cutoff))
            {
                continue;
            }

            var key = ToKey(path);
            if (key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
            {
                yield return (path, key);
            }
        }
    }

    private static string ResolveRootPath(string configuredRootPath)
    {
        var rootPath = string.IsNullOrWhiteSpace(configuredRootPath) ? DefaultRootDirectoryName : configuredRootPath;
        if (!Path.IsPathRooted(rootPath))
        {
            rootPath = Path.Combine(AppContext.BaseDirectory, rootPath);
        }

        return Path.GetFullPath(rootPath);
    }

    private string ToSafePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Object key must not be empty.", nameof(key));
        }

        var relativePath = key.Replace(KeySeparator, Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException("Object key resolves outside of the storage root.", nameof(key));
        }

        return fullPath;
    }

    private string ToKey(string fullPath)
    {
        return Path.GetRelativePath(_rootPath, fullPath).Replace(Path.DirectorySeparatorChar, KeySeparator);
    }
}
