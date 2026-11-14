// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates the storage root and the inbox folder of every ERP drop point, so the folder Klacksy and the Import card
/// show exists from startup on and not only after the first import run or health check. Uses the same
/// CheckHealthAsync the folder health skill uses (creates missing folders, never changes permissions). Without a drop
/// point yet (fresh installation) the default prefix is created, the one ErpDefaultDropPointProvider will use.
/// </summary>
/// <param name="repository">Reads the configured drop points; a failing read falls back to the default prefix</param>
/// <param name="objectStorageService">Creates the root and prefix folders</param>
/// <param name="logger">Reports folders that could not be created or written</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Imports;

public class ErpDropFolderInitializer
{
    private readonly IErpDropPointRepository _repository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<ErpDropFolderInitializer> _logger;

    public ErpDropFolderInitializer(
        IErpDropPointRepository repository,
        IObjectStorageService objectStorageService,
        ILogger<ErpDropFolderInitializer> logger)
    {
        _repository = repository;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the ERP drop folders exist in object storage and reports the health result.
    /// </summary>
    /// <param name="cancellationToken">Cancels the folder check</param>
    public async Task<ObjectStorageHealthResult> EnsureFoldersAsync(CancellationToken cancellationToken = default)
    {
        var prefixes = await ReadPrefixesAsync();
        var health = await _objectStorageService.CheckHealthAsync(prefixes, cancellationToken);

        if (!health.RootDirectoryReady || !health.IsWritable || health.Prefixes.Any(prefix => !prefix.Ready))
        {
            _logger.LogWarning(
                "ERP drop folder under {RootPath} is not ready (root ready: {RootReady}, writable: {Writable}, missing prefixes: {Prefixes})",
                health.RootPath,
                health.RootDirectoryReady,
                health.IsWritable,
                string.Join(", ", health.Prefixes.Where(prefix => !prefix.Ready).Select(prefix => prefix.Prefix)));
        }

        return health;
    }

    private async Task<IReadOnlyList<string>> ReadPrefixesAsync()
    {
        var prefixes = new List<string>();
        try
        {
            prefixes.AddRange((await _repository.List())
                .Where(dropPoint => !string.IsNullOrWhiteSpace(dropPoint.BucketPrefix))
                .Select(dropPoint => ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix))
                .Distinct(StringComparer.Ordinal));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ERP drop points could not be read; creating only the default drop folder");
        }

        if (prefixes.Count == 0)
        {
            prefixes.Add(ErpImportStorageKeys.NormalizePrefix(ErpDropPointDefaults.BucketPrefix));
        }

        return prefixes;
    }
}
