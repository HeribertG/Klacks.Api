// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Summarizes the dashboard location map as compact counts: how many active clients have a
/// current address, broken down by client type (employee / external / customer), by country
/// and by the most frequent cities, plus how many are geocoded. The group-visibility rules
/// of the calling user apply. Complementary to get_dashboard_summary (coverage figures).
/// </summary>
/// <param name="clientType">Optional filter: employee, external or customer</param>
/// <param name="topCities">Maximum number of cities to return (default 10, capped at 50)</param>

using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_client_locations_overview")]
public class GetClientLocationsOverviewSkill : BaseSkillImplementation
{
    private const string ClientTypeParameter = "clientType";
    private const string TopCitiesParameter = "topCities";
    private const int DefaultTopCities = 10;
    private const int MinTopCities = 1;
    private const int MaxTopCities = 50;
    private const string UnknownLocationLabel = "(unknown)";

    private static readonly Dictionary<string, EntityTypeEnum> ClientTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["employee"] = EntityTypeEnum.Employee,
        ["external"] = EntityTypeEnum.ExternEmp,
        ["customer"] = EntityTypeEnum.Customer
    };

    private readonly IMediator _mediator;

    public GetClientLocationsOverviewSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        EntityTypeEnum? typeFilter = null;
        var clientTypeValue = GetParameter<string>(parameters, ClientTypeParameter);
        if (!string.IsNullOrWhiteSpace(clientTypeValue))
        {
            if (!ClientTypeMap.TryGetValue(clientTypeValue.Trim(), out var parsedType))
            {
                return SkillResult.Error(
                    $"Invalid clientType '{clientTypeValue}'. Allowed values: {string.Join(", ", ClientTypeMap.Keys)}.");
            }

            typeFilter = parsedType;
        }

        var topCities = GetParameter<int?>(parameters, TopCitiesParameter) ?? DefaultTopCities;
        topCities = Math.Clamp(topCities, MinTopCities, MaxTopCities);

        var locations = (await _mediator.Send(new GetClientLocationsQuery(), cancellationToken))?.ToList() ?? [];

        if (typeFilter.HasValue)
        {
            locations = locations.Where(l => l.Type == (int)typeFilter.Value).ToList();
        }

        var byType = locations
            .GroupBy(l => l.Type)
            .Select(g => new { Type = ((EntityTypeEnum)g.Key).ToString(), Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var byCountry = locations
            .GroupBy(l => Label(l.CurrentAddress?.Country))
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var cities = locations
            .GroupBy(l => new
            {
                City = Label(l.CurrentAddress?.City),
                Country = Label(l.CurrentAddress?.Country)
            })
            .Select(g => new { g.Key.City, g.Key.Country, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.City)
            .Take(topCities)
            .ToList();

        var geocodedCount = locations.Count(l =>
            l.CurrentAddress?.Latitude.HasValue == true && l.CurrentAddress?.Longitude.HasValue == true);

        var data = new
        {
            TotalWithAddress = locations.Count,
            GeocodedCount = geocodedCount,
            ClientTypeFilter = typeFilter?.ToString(),
            ByType = byType,
            ByCountry = byCountry,
            TopCities = cities
        };

        var filterNote = typeFilter.HasValue ? $" of type {typeFilter}" : string.Empty;
        var message =
            $"Found {locations.Count} client location(s){filterNote} across {byCountry.Count} country/countries " +
            $"and {cities.Count} listed city/cities; {geocodedCount} geocoded.";

        return SkillResult.SuccessResult(data, message);
    }

    private static string Label(string? value)
        => string.IsNullOrWhiteSpace(value) ? UnknownLocationLabel : value;
}
