// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// File-backed <see cref="ICountryRegionProvider"/>: reads GeoData/Regions/{COUNTRY}.json (ISO alpha-2 name;
/// alpha-3 input such as "USA" is normalised first) once per country and caches the state-code-to-region
/// map for the process lifetime. Every region carries a stable key and its display name per language. A
/// missing file means the country has no region level (two-level tree); a malformed file is a shipping
/// defect and fails loudly with the file name: invalid JSON, a region without key, a duplicate key, a
/// region missing a core language name, a region without states, or a state listed in two regions.
/// </summary>
/// <param name="rootDirectory">Absolute directory that holds the per-country region files</param>
using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Geo;
using Klacks.Api.Domain.Models.Geo;

namespace Klacks.Api.Infrastructure.Services.Geo;

public sealed class FileCountryRegionProvider : ICountryRegionProvider
{
    private const string InvalidJsonMessageTemplate = "Country region file '{0}' is not valid JSON.";
    private const string NullDocumentMessageTemplate = "Country region file '{0}' is empty or contains no JSON object.";
    private const string RegionWithoutKeyMessageTemplate = "Country region file '{0}' contains a region without a key.";
    private const string DuplicateKeyMessageTemplate = "Country region file '{0}' contains the region key '{1}' more than once.";
    private const string MissingLanguageMessageTemplate = "Country region file '{0}': region '{1}' has no name in the core language '{2}'.";
    private const string RegionWithoutStatesMessageTemplate = "Country region file '{0}': region '{1}' lists no states.";
    private const string StateInTwoRegionsMessageTemplate = "Country region file '{0}': state '{1}' is listed in region '{2}' and in region '{3}'.";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly IReadOnlyDictionary<string, CountryRegion> Empty =
        new Dictionary<string, CountryRegion>(StringComparer.OrdinalIgnoreCase);

    private readonly string _rootDirectory;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, CountryRegion>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public FileCountryRegionProvider(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public static FileCountryRegionProvider FromBaseDirectory() =>
        new(Path.Combine(AppContext.BaseDirectory, CountryRegionFileLayout.Directory));

    public Task<IReadOnlyDictionary<string, CountryRegion>> GetRegionByStateAsync(
        string countryCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Task.FromResult(Empty);
        }

        var code = CountryCodeNormalizer.ToAlpha2(countryCode)!.ToUpperInvariant();
        return Task.FromResult(_cache.GetOrAdd(code, Load));
    }

    private IReadOnlyDictionary<string, CountryRegion> Load(string countryCode)
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

        var map = new Dictionary<string, CountryRegion>(StringComparer.OrdinalIgnoreCase);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in file.Regions)
        {
            var region = ToRegion(entry, path, seenKeys);
            foreach (var state in entry.States.Select(s => s.Trim().ToUpperInvariant()))
            {
                if (map.TryGetValue(state, out var other))
                {
                    throw new InvalidDataException(string.Format(StateInTwoRegionsMessageTemplate, path, state, other.Key, region.Key));
                }

                map[state] = region;
            }
        }

        return map;
    }

    private static CountryRegion ToRegion(CountryRegionEntry entry, string path, HashSet<string> seenKeys)
    {
        var key = entry.Key?.Trim() ?? string.Empty;
        if (key.Length == 0)
        {
            throw new InvalidDataException(string.Format(RegionWithoutKeyMessageTemplate, path));
        }

        if (!seenKeys.Add(key))
        {
            throw new InvalidDataException(string.Format(DuplicateKeyMessageTemplate, path, key));
        }

        var names = new MultiLanguage();
        foreach (var (language, name) in entry.Names ?? [])
        {
            names.SetValue(language.Trim(), name?.Trim());
        }

        var missing = MultiLanguage.CoreLanguages.FirstOrDefault(language => string.IsNullOrEmpty(names.GetValue(language)));
        if (missing != null)
        {
            throw new InvalidDataException(string.Format(MissingLanguageMessageTemplate, path, key, missing));
        }

        if (entry.States == null || entry.States.Count == 0 || entry.States.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException(string.Format(RegionWithoutStatesMessageTemplate, path, key));
        }

        return new CountryRegion(key, names);
    }
}
