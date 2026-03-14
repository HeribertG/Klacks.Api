// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Looks up a location by city name or zip code and returns canton/state information.
/// Used by the guided workflow to determine canton before filtering contracts and groups.
/// </summary>
/// <param name="city">City name to look up (e.g. "Bern")</param>
/// <param name="zip">Postal code to look up (e.g. 3011)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("lookup_location")]
public class LookupLocationSkill : BaseSkillImplementation
{
    private const string DefaultCountry = "Schweiz";
    private const int MaxResults = 10;

    private readonly IPostcodeChRepository _postcodeRepository;

    public LookupLocationSkill(IPostcodeChRepository postcodeRepository)
    {
        _postcodeRepository = postcodeRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var city = GetParameter<string>(parameters, "city");
        var zip = GetParameter<int?>(parameters, "zip");

        if (string.IsNullOrWhiteSpace(city) && zip == null)
        {
            return SkillResult.Error("Either 'city' or 'zip' parameter is required.");
        }

        var results = zip.HasValue
            ? await _postcodeRepository.GetByZipAsync(zip.Value)
            : await _postcodeRepository.GetByCityAsync(city!.Trim());

        if (results.Count == 0)
        {
            var searchTerm = zip.HasValue ? $"zip {zip}" : $"city '{city}'";
            return SkillResult.Error($"No location found for {searchTerm}. Try a different search term or use web_search for international locations.");
        }

        var distinctLocations = results
            .GroupBy(r => new { r.City, r.State })
            .Select(g => new
            {
                City = g.Key.City,
                Canton = g.Key.State,
                Country = DefaultCountry,
                ZipCodes = g.Select(r => r.Zip).Distinct().OrderBy(z => z).ToList()
            })
            .OrderBy(l => l.City)
            .Take(MaxResults)
            .ToList();

        var resultData = new
        {
            Locations = distinctLocations,
            TotalFound = distinctLocations.Count,
            SearchedBy = zip.HasValue ? "zip" : "city"
        };

        if (distinctLocations.Count == 1)
        {
            var loc = distinctLocations[0];
            return SkillResult.SuccessResult(resultData,
                $"{loc.City} is in canton {loc.Canton}, {loc.Country} (ZIP: {string.Join(", ", loc.ZipCodes)}).");
        }

        var summary = string.Join(", ", distinctLocations.Select(l => $"{l.City} ({l.Canton})"));
        return SkillResult.SuccessResult(resultData,
            $"Found {distinctLocations.Count} location(s): {summary}.");
    }
}
