// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Redirects writable paths below Hosting:DataRoot for native installations, where the program directory is
/// versioned and read-only. Without a DataRoot every method returns exactly what the caller used before, so
/// container deployments (volumes mounted below /app) are unaffected.
/// </summary>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Services.Exports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.Infrastructure.Hosting;

public static class DataRootPaths
{
    public const string ImagesKey = "CurrentPaths:Images";
    public const string DocumentsKey = "CurrentPaths:Documents";
    public const string ErpRootKey = ErpObjectStorageOptions.SectionName + ":RootPath";
    public const string PayrollRootKey = PayrollObjectStorageOptions.SectionName + ":RootPath";

    private const string DefaultImages = "Images";
    private const string DefaultDocuments = "Documents";

    private static readonly IReadOnlyList<(string Key, string Default)> WritableKeys =
    [
        (ImagesKey, DefaultImages),
        (DocumentsKey, DefaultDocuments),
        (ErpRootKey, ErpObjectStorageOptions.DefaultRootPath),
        (PayrollRootKey, PayrollObjectStorageOptions.DefaultRootPath),
        (KnowledgeIndexConstants.ModelsRootConfigKey, KnowledgeIndexConstants.ModelsCacheSubdirectory),
    ];

    public static string? GetDataRoot(IConfiguration configuration)
    {
        var dataRoot = configuration[NativeHostingOptions.DataRootKey];
        return string.IsNullOrWhiteSpace(dataRoot) ? null : Path.GetFullPath(dataRoot);
    }

    /// <summary>
    /// Anchors a relative writable path at DataRoot when one is configured, otherwise at the caller's previous base.
    /// Absolute paths are returned unchanged.
    /// </summary>
    public static string Resolve(IConfiguration configuration, string path, string fallbackBase)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(GetDataRoot(configuration) ?? fallbackBase, path);
    }

    /// <summary>
    /// Configuration overrides that turn every relative writable path setting into an absolute path below DataRoot.
    /// Empty without DataRoot; settings that are already absolute (for example from environment variables) are kept.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> BuildConfigurationOverrides(IConfiguration configuration)
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var dataRoot = GetDataRoot(configuration);
        if (dataRoot is null)
        {
            return overrides;
        }

        foreach (var (key, defaultValue) in WritableKeys)
        {
            var configured = configuration[key];
            var value = string.IsNullOrWhiteSpace(configured) ? defaultValue : configured;
            if (!Path.IsPathRooted(value))
            {
                overrides[key] = Path.Combine(dataRoot, value);
            }
        }

        return overrides;
    }
}
