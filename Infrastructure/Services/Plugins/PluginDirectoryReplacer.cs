// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Replaces a plugin directory with the content of a verified archive without leaving a half-written
/// plugin behind. The archive is extracted into a staging directory next to the plugin root (same file
/// system, outside the directory the plugin discovery scans); only after a complete extraction is the old
/// plugin directory moved aside, the staging directory moved into place and the old one deleted. If moving
/// the new directory in fails, the old one is moved back. The staging directory is removed on every path.
/// ZipFile.ExtractToDirectory refuses entries that would land outside the staging directory.
/// </summary>
/// <param name="archiveContent">Verified, limit-checked plugin archive</param>
/// <param name="pluginDirectory">Final directory of the plugin under the plugin root</param>
/// <param name="workDirectory">Directory next to the plugin root for staging and the previous version</param>
/// <param name="logger">Receives a warning when a staging or previous directory cannot be removed</param>
using System.IO.Compression;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public static class PluginDirectoryReplacer
{
    private const string StagingPrefix = ".plugin-staging-";
    private const string PreviousPrefix = ".plugin-previous-";
    private const string GuidFormat = "N";

    public static void Replace(byte[] archiveContent, string pluginDirectory, string workDirectory, ILogger logger)
    {
        var token = Guid.NewGuid().ToString(GuidFormat);
        var staging = Path.Combine(workDirectory, StagingPrefix + token);
        var previous = Path.Combine(workDirectory, PreviousPrefix + token);

        try
        {
            Directory.CreateDirectory(workDirectory);
            using (var stream = new MemoryStream(archiveContent, writable: false))
            {
                ZipFile.ExtractToDirectory(stream, staging);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(pluginDirectory)!);
            if (Directory.Exists(pluginDirectory))
            {
                Directory.Move(pluginDirectory, previous);
            }

            try
            {
                Directory.Move(staging, pluginDirectory);
            }
            catch
            {
                if (Directory.Exists(previous) && !Directory.Exists(pluginDirectory))
                {
                    Directory.Move(previous, pluginDirectory);
                }

                throw;
            }

            DeleteQuietly(previous, logger);
        }
        finally
        {
            DeleteQuietly(staging, logger);
        }
    }

    private static void DeleteQuietly(string directory, ILogger logger)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Plugin work directory {Directory} could not be removed", directory);
        }
    }
}
