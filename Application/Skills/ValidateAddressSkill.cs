// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("validate_address")]
public class ValidateAddressSkill : BaseSkillImplementation
{
    private static readonly string[] PostalCodeParameterNames =
        ["postalCode", "zip", "plz", "postalcode", "postal_code", "postleitzahl"];

    private static readonly Regex PostalCodePattern = new(@"\b(\d{4,5})\b", RegexOptions.Compiled);

    private readonly IGeocodingService _geocodingService;

    public ValidateAddressSkill(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    private static string? ResolvePostalCode(Dictionary<string, object> parameters)
    {
        foreach (var name in PostalCodeParameterNames)
        {
            var value = GetParameter<string>(parameters, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static (string? PostalCode, string? City) RecoverPostalCodeAndCity(string? postalCode, string? city, string? street)
    {
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            return (postalCode, city);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityMatch = PostalCodePattern.Match(city);
            if (cityMatch.Success)
            {
                var remainder = city.Remove(cityMatch.Index, cityMatch.Length).Trim().Trim(',').Trim();
                return (cityMatch.Groups[1].Value, string.IsNullOrWhiteSpace(remainder) ? city : remainder);
            }
        }

        if (!string.IsNullOrWhiteSpace(street))
        {
            var streetMatch = PostalCodePattern.Match(street);
            if (streetMatch.Success)
            {
                var recoveredZip = streetMatch.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(city))
                {
                    var tail = street[(streetMatch.Index + streetMatch.Length)..].Trim().Trim(',').Trim();
                    if (!string.IsNullOrWhiteSpace(tail))
                    {
                        city = tail;
                    }
                }

                return (recoveredZip, city);
            }
        }

        return (postalCode, city);
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var street = GetParameter<string>(parameters, "street");
        var city = GetParameter<string>(parameters, "city");
        var country = GetParameter<string>(parameters, "country", "Schweiz");
        var postalCode = ResolvePostalCode(parameters);

        (postalCode, city) = RecoverPostalCodeAndCity(postalCode, city, street);

        if (string.IsNullOrWhiteSpace(postalCode) || string.IsNullOrWhiteSpace(city))
        {
            return new SkillResult
            {
                Success = true,
                Data = new
                {
                    IsValid = false,
                    ExactMatch = false,
                    Message = "Postal code and city are required to validate an address."
                },
                Message = "I still need the postal code and city. Please provide the address as 'street, postal code city' (e.g. 'Kirchstrasse 52, 3097 Liebefeld').",
                Type = SkillResultType.Data
            };
        }

        var fullAddress = string.IsNullOrEmpty(street)
            ? $"{postalCode} {city}"
            : $"{street}, {postalCode} {city}";

        try
        {
            var validation = await _geocodingService.ValidateExactAddressAsync(street, postalCode, city, country ?? "Schweiz");
            var state = validation.State;

            var zipInfo = state != null
                ? $"The ZIP code {postalCode} is VALID and belongs to state/region {state}."
                : $"The ZIP code {postalCode} could not be mapped to a state/region.";

            if (!validation.Found)
            {
                return new SkillResult
                {
                    Success = true,
                    Data = new
                    {
                        IsValid = false,
                        ExactMatch = false,
                        InputAddress = fullAddress,
                        State = state,
                        ZipValid = state != null,
                        MatchType = validation.MatchType,
                        Message = $"Address '{fullAddress}' could not be found via geocoding. {zipInfo} The issue is likely the street/house number or the ZIP+city combination."
                    },
                    Message = $"Address '{fullAddress}' was not found via geocoding. {zipInfo} Please check street and house number.",
                    Type = SkillResultType.Data
                };
            }

            if (!validation.ExactMatch)
            {
                return new SkillResult
                {
                    Success = true,
                    Data = new
                    {
                        IsValid = false,
                        ExactMatch = false,
                        InputAddress = fullAddress,
                        State = state,
                        ZipValid = state != null,
                        MatchType = validation.MatchType,
                        ReturnedAddress = validation.ReturnedAddress,
                        Latitude = validation.Latitude,
                        Longitude = validation.Longitude,
                        Message = $"ZIP code and city matched, but street or house number could not be verified. {zipInfo}"
                    },
                    Message = $"ZIP {postalCode} and city {city} are CORRECT (found: '{validation.ReturnedAddress}'). {zipInfo} Street or house number could not be verified.",
                    Type = SkillResultType.Data
                };
            }

            return SkillResult.SuccessResult(
                new
                {
                    IsValid = true,
                    ExactMatch = true,
                    InputAddress = fullAddress,
                    ReturnedAddress = validation.ReturnedAddress,
                    Latitude = validation.Latitude,
                    Longitude = validation.Longitude,
                    State = state,
                    Zip = postalCode,
                    City = city,
                    Country = country,
                    MatchType = validation.MatchType
                },
                $"Address validated successfully. Exact match found: '{validation.ReturnedAddress}'. State: {state}");
        }
        catch (Exception ex)
        {
            return new SkillResult
            {
                Success = true,
                Data = new
                {
                    IsValid = false,
                    ExactMatch = false,
                    InputAddress = fullAddress,
                    Error = ex.Message
                },
                Message = $"Could not validate address via geocoding.",
                Type = SkillResultType.Data
            };
        }
    }
}
