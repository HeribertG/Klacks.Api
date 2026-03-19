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

        var canton = DetectCantonFromPostalCode(postalCode);

        var fullAddress = string.IsNullOrEmpty(street)
            ? $"{postalCode} {city}"
            : $"{street}, {postalCode} {city}";

        try
        {
            var validation = await _geocodingService.ValidateExactAddressAsync(street, postalCode, city, country);

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
                        Canton = canton,
                        MatchType = validation.MatchType,
                        Message = $"Address '{fullAddress}' could not be found. Please check street name, house number and postal code."
                    },
                    Message = $"ACHTUNG: Die Adresse '{fullAddress}' wurde NICHT gefunden. Bitte Strasse, Hausnummer und PLZ prüfen. Kanton aus PLZ: {canton ?? "unbekannt"}",
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
                        Canton = canton,
                        MatchType = validation.MatchType,
                        ReturnedAddress = validation.ReturnedAddress,
                        Latitude = validation.Latitude,
                        Longitude = validation.Longitude,
                        Message = $"Address '{fullAddress}' was NOT found exactly. Only the city/postal code matched. The street or house number may not exist."
                    },
                    Message = $"ACHTUNG: Die Adresse '{fullAddress}' wurde NICHT exakt gefunden. Nur PLZ/Ort stimmen. Gefunden: '{validation.ReturnedAddress}'. Kanton: {canton ?? "unbekannt"}",
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
                    Canton = canton,
                    PostalCode = postalCode,
                    City = city,
                    Country = country,
                    MatchType = validation.MatchType
                },
                $"Address validated successfully. Exact match found: '{validation.ReturnedAddress}'. Canton: {canton}");
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
                    Canton = canton,
                    Error = ex.Message
                },
                Message = $"Could not validate address via geocoding. Canton: {canton ?? "unknown"}",
                Type = SkillResultType.Data
            };
        }
    }

    private static readonly (int From, int To, string Canton)[] CantonRanges =
    [
        (1000, 1200, "VD"),
        (1200, 1300, "GE"),
        (1300, 1400, "VD"),
        (1400, 1500, "VD"),
        (1500, 1600, "VD"),
        (1600, 1700, "FR"),
        (1700, 1800, "FR"),
        (1800, 1900, "VD"),
        (1900, 2000, "VS"),
        (2000, 2100, "NE"),
        (2100, 2200, "NE"),
        (2200, 2300, "NE"),
        (2300, 2400, "NE"),
        (2400, 2500, "NE"),
        (2500, 2600, "BE"),
        (2600, 2700, "BE"),
        (2700, 2800, "JU"),
        (2800, 2900, "JU"),
        (2900, 3000, "JU"),
        (3000, 4000, "BE"),
        (4000, 4100, "BS"),
        (4100, 4200, "BL"),
        (4200, 4300, "BL"),
        (4300, 4400, "BL"),
        (4400, 4500, "SO"),
        (4500, 4600, "SO"),
        (4600, 4700, "SO"),
        (4700, 4800, "SO"),
        (4800, 4900, "AG"),
        (4900, 5000, "BE"),
        (5000, 6000, "AG"),
        (6000, 6100, "LU"),
        (6100, 6200, "LU"),
        (6200, 6300, "LU"),
        (6300, 6400, "ZG"),
        (6400, 6500, "SZ"),
        (6500, 6600, "TI"),
        (6600, 6700, "TI"),
        (6700, 6800, "TI"),
        (6800, 6900, "TI"),
        (6900, 7000, "TI"),
        (7000, 7200, "GR"),
        (7200, 7300, "GR"),
        (7300, 7400, "GR"),
        (7400, 7500, "GR"),
        (7500, 7600, "GR"),
        (7600, 7700, "GR"),
        (7700, 8000, "GR"),
        (8000, 8100, "ZH"),
        (8100, 8200, "ZH"),
        (8200, 8300, "ZH"),
        (8300, 8400, "ZH"),
        (8400, 8500, "ZH"),
        (8500, 8600, "TG"),
        (8600, 8700, "ZH"),
        (8700, 8800, "SZ"),
        (8800, 8900, "SZ"),
        (8900, 9000, "SG"),
        (9000, 9100, "SG"),
        (9100, 9200, "SG"),
        (9200, 9300, "TG"),
        (9300, 9400, "SG"),
        (9400, 9500, "SG"),
        (9500, 9600, "SG"),
        (9600, 9700, "SG"),
        (9700, 9800, "SG"),
        (9800, 9900, "SG"),
        (9900, 10000, "AR"),
    ];

    private static string? DetectCantonFromPostalCode(string postalCode)
    {
        if (string.IsNullOrEmpty(postalCode) || !int.TryParse(postalCode, out var plz))
            return null;

        foreach (var (from, to, canton) in CantonRanges)
        {
            if (plz >= from && plz < to)
                return canton;
        }

        return null;
    }
}
