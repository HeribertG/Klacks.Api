// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// File-backed <see cref="ICountryRegionProvider"/>: reads GeoData/Regions/{COUNTRY}.json once per
/// country and caches the state-code-to-region map for the process lifetime. A missing file means the
/// country has no region level (two-level tree); a malformed file is a shipping defect and fails loudly
/// with the file name.
/// </summary>
/// <param name="rootDirectory">Absolute directory that holds the per-country region files</param>
using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Geo;

namespace Klacks.Api.Infrastructure.Services.Geo;

public sealed class FileCountryRegionProvider : ICountryRegionProvider
{
    private const string InvalidJsonMessageTemplate = "Country region file '{0}' is not valid JSON.";
    private const string NullDocumentMessageTemplate = "Country region file '{0}' is empty or contains no JSON object.";
    private const string RegionWithoutNameMessageTemplate = "Country region file '{0}' contains a region without a name.";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly string _rootDirectory;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public FileCountryRegionProvider(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public static FileCountryRegionProvider FromBaseDirectory() =>
        new(Path.Combine(AppContext.BaseDirectory, CountryRegionFileLayout.Directory));

    public Task<IReadOnlyDictionary<string, string>> GetRegionByStateAsync(
        string countryCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Task.FromResult(Empty);
        }

        var code = countryCode.Trim().ToUpperInvariant();
        return Task.FromResult(_cache.GetOrAdd(code, Load));
    }

    private IReadOnlyDictionary<string, string> Load(string countryCode)
    {
        var path = Path.Combine(_rootDirectory, countryCode + CountryRegionFileLayout.FileExtension);
        if (!File.Exists(path))
        {
            return Empty;
        }

        CountryRegionFile? file;
        try
        {
            file = JsonSerializer.Deserialize<CountryRegionFile>(File.ReadAllText(path), SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(string.Format(InvalidJsonMessageTemplate, path), exception);
        }

        if (file == null)
        {
            throw new InvalidDataException(string.Format(NullDocumentMessageTemplate, path));
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in file.Regions)
        {
            if (string.IsNullOrWhiteSpace(region.Name))
            {
                throw new InvalidDataException(string.Format(RegionWithoutNameMessageTemplate, path));
            }

            foreach (var state in region.States)
            {
                map[state.Trim().ToUpperInvariant()] = region.Name.Trim();
            }
        }

        return map;
    }
}
