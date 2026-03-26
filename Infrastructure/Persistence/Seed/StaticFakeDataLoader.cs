// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads pre-generated SQL dump files from embedded resources for fast seed data import.
/// </summary>

using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace Klacks.Api.Data.Seed;

public static class StaticFakeDataLoader
{
    private const string ResourcePrefix = "Klacks.Api.Infrastructure.Persistence.Seed.Dumps.";

    public static string LoadSeedDump(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{ResourcePrefix}{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed dump resource '{resourceName}' not found.");

        if (fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        using var directReader = new StreamReader(stream, Encoding.UTF8);
        return directReader.ReadToEnd();
    }
}
