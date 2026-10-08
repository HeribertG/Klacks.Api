// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// File-system storage of the payroll export artifacts below PayrollObjectStorage:RootPath. Reuses the path-safe,
/// atomic-rename file storage of the ERP import with its own root, so payroll files and the ERP drop zone never
/// share a directory or a volume. A missing artifact is reported as null instead of an IO exception.
/// </summary>
/// <param name="options">Holds the payroll storage root</param>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Services.Exports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Services.Imports;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PayrollArtifactStorage : IPayrollArtifactStorage
{
    private readonly IObjectStorageService _storage;

    public PayrollArtifactStorage(IOptions<PayrollObjectStorageOptions> options)
    {
        var rootPath = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? PayrollObjectStorageOptions.DefaultRootPath
            : options.Value.RootPath;
        var rootOptions = new ErpObjectStorageOptions { RootPath = rootPath };
        _storage = new FileSystemObjectStorageService(Options.Create(rootOptions));
    }

    public async Task UploadAsync(string key, byte[] content, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content, writable: false);
        await _storage.UploadAsync(key, stream, cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = await _storage.DownloadAsync(key, cancellationToken);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return _storage.DeleteAsync(key, cancellationToken);
    }
}