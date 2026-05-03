// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly IGeocodingService _geocodingService;

    public ValidateAddressSkill(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var street = GetParameter<string>(parameters, "street");
        var postalCode = GetRequiredString(parameters, "postalCode");
        var city = GetRequiredString(parameters, "city");
        var country = GetParameter<string>(parameters, "country", "Schweiz");

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
