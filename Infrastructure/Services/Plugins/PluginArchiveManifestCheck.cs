// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks, before a marketplace plugin archive is extracted, that its root manifest.json declares exactly
/// the requested plugin name (ordinal comparison, the name is also the directory name). A missing,
/// unreadable or differently named manifest fails the check, so an archive can never be installed under
/// another plugin's directory.
/// </summary>
using System.IO.Compression;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Plugins;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public static class PluginArchiveManifestCheck
{
    private static readonly JsonSerializerOptions ManifestReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool DeclaresPlugin(byte[] archiveContent, string expectedName)
    {
        try
        {
            using var stream = new MemoryStream(archiveContent, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.FullName, FeaturePluginConstants.ManifestFileName, StringComparison.Ordinal));
            if (entry == null)
            {
                return false;
            }

            using var manifestStream = entry.Open();
            var manifest = JsonSerializer.Deserialize<FeaturePluginManifest>(manifestStream, ManifestReadOptions);
            return manifest != null && string.Equals(manifest.Name, expectedName, StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException or NotSupportedException)
        {
            return false;
        }
    }
}
