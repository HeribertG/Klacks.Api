// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates company-rule intake values against the catalog: it rejects unknown parameters, empty
/// values, malformed times/numbers, out-of-range numbers and enum values outside the allowed set, and
/// reports the still-missing required parameters for a draft including the conditional hours-threshold
/// and the "at least one surcharge parameter" rule.
/// </summary>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class CompanyRuleDraftValidator : ICompanyRuleDraftValidator
{
    private readonly ICompanyRuleParameterCatalog _catalog;

    public CompanyRuleDraftValidator(ICompanyRuleParameterCatalog catalog)
    {
        _catalog = catalog;
    }

    public CompanyRuleValidationResult Validate(CompanyRuleKind kind, string parameterName, string value)
    {
        var definition = _catalog.FindParameter(kind, parameterName);
        if (definition is null)
        {
            return CompanyRuleValidationResult.Invalid($"Unknown parameter '{parameterName}' for rule kind {kind}.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return CompanyRuleValidationResult.Invalid($"Parameter '{parameterName}' must not be empty.");
        }

        return definition.DataType switch
        {
            CompanyRuleParameterDataType.Time => ValidateTime(parameterName, value),
            CompanyRuleParameterDataType.Integer => ValidateInteger(definition, value),
            CompanyRuleParameterDataType.Decimal => ValidateDecimal(definition, value),
            CompanyRuleParameterDataType.Enum => ValidateEnum(definition, value),
            _ => CompanyRuleValidationResult.Ok()
        };
    }

    public IReadOnlyList<string> GetMissingRequired(CompanyRuleDraft draft)
    {
        var missing = new List<string>();

        foreach (var definition in _catalog.GetParameters(draft.Kind))
        {
            if (definition.Required && !HasValue(draft, definition.Name))
            {
                missing.Add(definition.Name);
            }
        }

        AddConditionalHoursThreshold(draft, missing);
        AddAtLeastOneSurchargeParameter(draft, missing);

        return missing;
    }

    private static void AddConditionalHoursThreshold(CompanyRuleDraft draft, List<string> missing)
    {
        if (draft.Kind != CompanyRuleKind.CounterRule)
        {
            return;
        }

        if (!draft.Parameters.TryGetValue(CompanyRuleParameterNames.EventType, out var eventType))
        {
            return;
        }

        if (string.Equals(eventType, nameof(CounterEventType.ShiftExceedingHours), StringComparison.OrdinalIgnoreCase)
            && !HasValue(draft, CompanyRuleParameterNames.HoursThreshold))
        {
            missing.Add(CompanyRuleParameterNames.HoursThreshold);
        }
    }

    private static void AddAtLeastOneSurchargeParameter(CompanyRuleDraft draft, List<string> missing)
    {
        if (draft.Kind != CompanyRuleKind.SurchargeSettings)
        {
            return;
        }

        if (!draft.Parameters.Any(p => !string.IsNullOrWhiteSpace(p.Value)))
        {
            missing.Add(CompanyRuleParameterNames.AtLeastOneSurchargeParameter);
        }
    }

    private static bool HasValue(CompanyRuleDraft draft, string name)
    {
        return draft.Parameters.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value);
    }

    private static CompanyRuleValidationResult ValidateTime(string parameterName, string value)
    {
        return DateTime.TryParseExact(value, CompanyRuleDraftDefaults.TimeOfDayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? CompanyRuleValidationResult.Ok()
            : CompanyRuleValidationResult.Invalid($"Parameter '{parameterName}' must be a time of day in {CompanyRuleDraftDefaults.TimeOfDayFormat} format.");
    }

    private static CompanyRuleValidationResult ValidateInteger(CompanyRuleParameterDefinition definition, string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return CompanyRuleValidationResult.Invalid($"Parameter '{definition.Name}' must be a whole number.");
        }

        return CheckRange(definition, parsed);
    }

    private static CompanyRuleValidationResult ValidateDecimal(CompanyRuleParameterDefinition definition, string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return CompanyRuleValidationResult.Invalid($"Parameter '{definition.Name}' must be a number.");
        }

        return CheckRange(definition, parsed);
    }

    private static CompanyRuleValidationResult CheckRange(CompanyRuleParameterDefinition definition, decimal parsed)
    {
        if (definition.Min.HasValue && parsed < definition.Min.Value)
        {
            return CompanyRuleValidationResult.Invalid($"Parameter '{definition.Name}' must be at least {definition.Min.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (definition.Max.HasValue && parsed > definition.Max.Value)
        {
            return CompanyRuleValidationResult.Invalid($"Parameter '{definition.Name}' must be at most {definition.Max.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        return CompanyRuleValidationResult.Ok();
    }

    private static CompanyRuleValidationResult ValidateEnum(CompanyRuleParameterDefinition definition, string value)
    {
        var allowed = definition.EnumValues ?? Array.Empty<string>();
        foreach (var candidate in allowed)
        {
            if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
            {
                return CompanyRuleValidationResult.Ok();
            }
        }

        return CompanyRuleValidationResult.Invalid($"Parameter '{definition.Name}' must be one of: {string.Join(", ", allowed)}.");
    }
}
