// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// File-system implementation of IFileUploadService for the images directory (CurrentPaths:Images).
/// Only names carried by a validated ImageFileTarget are touched; a name with a directory part is refused.
/// </summary>
/// <param name="configuration">Supplies the images directory below the application base directory</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.FileHandling;
using Klacks.Api.Infrastructure.Hosting;

namespace Klacks.Api.Infrastructure.FileHandling;

public class UploadFile : IFileUploadService
{
    private readonly IConfiguration _configuration;

    public UploadFile(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StoreImageAsync(ImageFileTarget target, Stream content, CancellationToken cancellationToken)
    {
        if (!target.StoredFileNames.Contains(target.FileName, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The upload file name is not one of the target's stored names.");
        }

        var directory = GetImagesDirectory(createIfMissing: true);

        foreach (var variant in target.StoredFileNames.Where(name => name != target.FileName))
        {
            DeleteIfExists(Path.Combine(directory, EnsurePlainFileName(variant)));
        }

        var path = Path.Combine(directory, EnsurePlainFileName(target.FileName));
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public async Task<byte[]?> ReadImageAsync(ImageFileTarget target, CancellationToken cancellationToken)
    {
        var directory = GetImagesDirectory(createIfMissing: false);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        foreach (var name in target.StoredFileNames)
        {
            var path = Path.Combine(directory, EnsurePlainFileName(name));
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path, cancellationToken);
            }
        }

        return null;
    }

    public void DeleteImage(ImageFileTarget target)
    {
        var directory = GetImagesDirectory(createIfMissing: false);
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var name in target.StoredFileNames)
        {
            DeleteIfExists(Path.Combine(directory, EnsurePlainFileName(name)));
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string EnsurePlainFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name)
        {
            throw new InvalidOperationException("The image file name must not contain a directory part.");
        }

        return name;
    }

    private string GetImagesDirectory(bool createIfMissing)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configuration[DataRootPaths.ImagesKey]!);

        if (createIfMissing && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return directory;
    }
}
