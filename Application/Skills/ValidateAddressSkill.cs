using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Services;

namespace Klacks.Api.Application.Skills;

public class ValidateAddressSkill : BaseSkill
{
    private readonly IGeocodingService _geocodingService;

    public override string Name => "validate_address";

    public override string Description =>
        "Validates an address and returns geocoding information (coordinates, formatted address). " +
        "Also detects the Swiss canton from postal code. Use this before creating an employee to verify the address exists.";

    public override SkillCategory Category => SkillCategory.Validation;

    public override IReadOnlyList<string> RequiredPermissions => Array.Empty<string>();

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "street",
            "Street name and house number",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "postalCode",
            "Postal code / ZIP code",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "city",
            "City name",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "country",
            "Country name (default: Schweiz)",
            SkillParameterType.String,
            Required: false,
            DefaultValue: "Schweiz")
    };

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

    private static string? DetectCantonFromPostalCode(string postalCode)
    {
        if (string.IsNullOrEmpty(postalCode) || postalCode.Length < 1)
            return null;

        if (!int.TryParse(postalCode, out var plz))
            return null;

        return plz switch
        {
            >= 1000 and < 1200 => "VD",
            >= 1200 and < 1300 => "GE",
            >= 1300 and < 1400 => "VD",
            >= 1400 and < 1500 => "VD",
            >= 1500 and < 1600 => "VD",
            >= 1600 and < 1700 => "FR",
            >= 1700 and < 1800 => "FR",
            >= 1800 and < 1900 => "VD",
            >= 1900 and < 2000 => "VS",
            >= 2000 and < 2100 => "NE",
            >= 2100 and < 2200 => "NE",
            >= 2200 and < 2300 => "NE",
            >= 2300 and < 2400 => "NE",
            >= 2400 and < 2500 => "NE",
            >= 2500 and < 2600 => "BE",
            >= 2600 and < 2700 => "BE",
            >= 2700 and < 2800 => "JU",
            >= 2800 and < 2900 => "JU",
            >= 2900 and < 3000 => "JU",
            >= 3000 and < 4000 => "BE",
            >= 4000 and < 4100 => "BS",
            >= 4100 and < 4200 => "BL",
            >= 4200 and < 4300 => "BL",
            >= 4300 and < 4400 => "BL",
            >= 4400 and < 4500 => "SO",
            >= 4500 and < 4600 => "SO",
            >= 4600 and < 4700 => "SO",
            >= 4700 and < 4800 => "SO",
            >= 4800 and < 4900 => "AG",
            >= 4900 and < 5000 => "BE",
            >= 5000 and < 6000 => "AG",
            >= 6000 and < 6100 => "LU",
            >= 6100 and < 6200 => "LU",
            >= 6200 and < 6300 => "LU",
            >= 6300 and < 6400 => "ZG",
            >= 6400 and < 6500 => "SZ",
            >= 6500 and < 6600 => "TI",
            >= 6600 and < 6700 => "TI",
            >= 6700 and < 6800 => "TI",
            >= 6800 and < 6900 => "TI",
            >= 6900 and < 7000 => "TI",
            >= 7000 and < 7200 => "GR",
            >= 7200 and < 7300 => "GR",
            >= 7300 and < 7400 => "GR",
            >= 7400 and < 7500 => "GR",
            >= 7500 and < 7600 => "GR",
            >= 7600 and < 7700 => "GR",
            >= 7700 and < 8000 => "GR",
            >= 8000 and < 8100 => "ZH",
            >= 8100 and < 8200 => "ZH",
            >= 8200 and < 8300 => "ZH",
            >= 8300 and < 8400 => "ZH",
            >= 8400 and < 8500 => "ZH",
            >= 8500 and < 8600 => "TG",
            >= 8600 and < 8700 => "ZH",
            >= 8700 and < 8800 => "SZ",
            >= 8800 and < 8900 => "SZ",
            >= 8900 and < 9000 => "SG",
            >= 9000 and < 9100 => "SG",
            >= 9100 and < 9200 => "SG",
            >= 9200 and < 9300 => "TG",
            >= 9300 and < 9400 => "SG",
            >= 9400 and < 9500 => "SG",
            >= 9500 and < 9600 => "SG",
            >= 9600 and < 9700 => "SG",
            >= 9700 and < 9800 => "SG",
            >= 9800 and < 9900 => "SG",
            >= 9900 and < 10000 => "AR",
            _ => null
        };
    }
}
