// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates planning-profile setup values against the catalog: it rejects unknown parameters, empty
/// values, malformed times/numbers, out-of-range numbers and enum values outside the allowed set, and
/// reports the still-missing required parameters for a draft — the base-industry choice plus the
/// conditional "at least one field override when starting from scratch" rule.
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

public class PlanningProfileDraftValidator : IPlanningProfileDraftValidator
{
    private readonly IPlanningProfileParameterCatalog _catalog;

    public PlanningProfileDraftValidator(IPlanningProfileParameterCatalog catalog)
    {
        _catalog = catalog;
    }

    public PlanningProfileValidationResult Validate(string parameterName, string value)
    {
        var definition = _catalog.FindParameter(parameterName);
        if (definition is null)
        {
            return PlanningProfileValidationResult.Invalid($"Unknown planning-profile parameter '{parameterName}'.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return PlanningProfileValidationResult.Invalid($"Parameter '{parameterName}' must not be empty.");
        }

        return definition.DataType switch
        {
            PlanningProfileParameterDataType.Time => ValidateTime(parameterName, value),
            PlanningProfileParameterDataType.Integer => ValidateInteger(definition, value),
            PlanningProfileParameterDataType.Decimal => ValidateDecimal(definition, value),
            PlanningProfileParameterDataType.Enum => ValidateEnum(definition, value),
            _ => PlanningProfileValidationResult.Ok()
        };
    }

    public IReadOnlyList<string> GetMissingRequired(PlanningProfileDraft draft)
    {
        var missing = new List<string>();

        foreach (var definition in _catalog.GetParameters())
        {
            if (definition.Required && !HasValue(draft, definition.Name))
            {
                missing.Add(definition.Name);
            }
        }

        AddConditionalScratchOverride(draft, missing);

        return missing;
    }

    private void AddConditionalScratchOverride(PlanningProfileDraft draft, List<string> missing)
    {
        if (!draft.Parameters.TryGetValue(PlanningProfileParameterNames.BaseIndustry, out var baseIndustry))
        {
            return;
        }

        if (!string.Equals(baseIndustry, PlanningProfileBaseChoices.Scratch, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!HasAnyOverride(draft))
        {
            missing.Add(PlanningProfileParameterNames.AtLeastOneOverride);
        }
    }

    private bool HasAnyOverride(PlanningProfileDraft draft)
    {
        foreach (var definition in _catalog.GetParameters())
        {
            if (IsOverrideParameter(definition.Name) && HasValue(draft, definition.Name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOverrideParameter(string name)
    {
        return name != PlanningProfileParameterNames.BaseIndustry
            && name != PlanningProfileParameterNames.ProfileName;
    }

    private static bool HasValue(PlanningProfileDraft draft, string name)
    {
        return draft.Parameters.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value);
    }

    private static PlanningProfileValidationResult ValidateTime(string parameterName, string value)
    {
        return DateTime.TryParseExact(value, PlanningProfileDraftDefaults.TimeOfDayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? PlanningProfileValidationResult.Ok()
            : PlanningProfileValidationResult.Invalid($"Parameter '{parameterName}' must be a time of day in {PlanningProfileDraftDefaults.TimeOfDayFormat} format.");
    }

    private static PlanningProfileValidationResult ValidateInteger(PlanningProfileParameterDefinition definition, string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return PlanningProfileValidationResult.Invalid($"Parameter '{definition.Name}' must be a whole number.");
        }

        return CheckRange(definition, parsed);
    }

    private static PlanningProfileValidationResult ValidateDecimal(PlanningProfileParameterDefinition definition, string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return PlanningProfileValidationResult.Invalid($"Parameter '{definition.Name}' must be a number.");
        }

        return CheckRange(definition, parsed);
    }

    private static PlanningProfileValidationResult CheckRange(PlanningProfileParameterDefinition definition, decimal parsed)
    {
        if (definition.Min.HasValue && parsed < definition.Min.Value)
        {
            return PlanningProfileValidationResult.Invalid($"Parameter '{definition.Name}' must be at least {definition.Min.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (definition.Max.HasValue && parsed > definition.Max.Value)
        {
            return PlanningProfileValidationResult.Invalid($"Parameter '{definition.Name}' must be at most {definition.Max.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        return PlanningProfileValidationResult.Ok();
    }

    private static PlanningProfileValidationResult ValidateEnum(PlanningProfileParameterDefinition definition, string value)
    {
        var allowed = definition.EnumValues ?? Array.Empty<string>();
        foreach (var candidate in allowed)
        {
            if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
            {
                return PlanningProfileValidationResult.Ok();
            }
        }

        return PlanningProfileValidationResult.Invalid($"Parameter '{definition.Name}' must be one of: {string.Join(", ", allowed)}.");
    }
}
