// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static, database-free source of truth for the parameters the company-rule intake collects per rule
/// kind. Surcharge parameters are all optional (the "at least one" rule is enforced by the validator);
/// counter-rule and custom-macro parameters carry their required flags, with the hours-threshold left
/// optional here because its requirement is conditional on the chosen event type.
/// </summary>

using System;
using System.Collections.Generic;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class CompanyRuleParameterCatalog : ICompanyRuleParameterCatalog
{
    private const decimal ZeroBound = 0m;
    private const int MinThreshold = 1;

    private static readonly IReadOnlyList<string> RateModeValues = new[]
    {
        CompanyRuleParameterValues.RateModeMultiplier,
        CompanyRuleParameterValues.RateModeFixedPerHour,
        CompanyRuleParameterValues.RateModeFixedPerShift
    };

    private static readonly IReadOnlyList<string> OvertimeRateModeValues = new[]
    {
        CompanyRuleParameterValues.RateModeMultiplier,
        CompanyRuleParameterValues.RateModeFixedPerHour
    };

    private static readonly IReadOnlyList<string> StackingModeValues = new[]
    {
        CompanyRuleParameterValues.StackingModeHighestWins,
        CompanyRuleParameterValues.StackingModeAdditive
    };

    private static readonly IReadOnlyList<string> OvertimeBasisValues = new[]
    {
        CompanyRuleParameterValues.OvertimeBasisDay,
        CompanyRuleParameterValues.OvertimeBasisWeek
    };

    private static readonly IReadOnlyList<string> EnforcementValues = new[]
    {
        CompanyRuleParameterValues.EnforcementWarn,
        CompanyRuleParameterValues.EnforcementBlock
    };

    private static readonly IReadOnlyList<string> EventTypeValues = Enum.GetNames(typeof(CounterEventType));
    private static readonly IReadOnlyList<string> PeriodValues = Enum.GetNames(typeof(CounterPeriod));
    private static readonly IReadOnlyList<string> MacroCategoryValues = Enum.GetNames(typeof(MacroCategoryEnum));

    private static readonly IReadOnlyDictionary<CompanyRuleKind, IReadOnlyList<CompanyRuleParameterDefinition>> Catalog =
        new Dictionary<CompanyRuleKind, IReadOnlyList<CompanyRuleParameterDefinition>>
        {
            [CompanyRuleKind.SurchargeSettings] = BuildSurchargeParameters(),
            [CompanyRuleKind.CounterRule] = BuildCounterRuleParameters(),
            [CompanyRuleKind.CustomMacro] = BuildCustomMacroParameters()
        };

    public IReadOnlyList<CompanyRuleParameterDefinition> GetParameters(CompanyRuleKind kind)
    {
        return Catalog.TryGetValue(kind, out var parameters)
            ? parameters
            : Array.Empty<CompanyRuleParameterDefinition>();
    }

    public CompanyRuleParameterDefinition? FindParameter(CompanyRuleKind kind, string parameterName)
    {
        foreach (var definition in GetParameters(kind))
        {
            if (string.Equals(definition.Name, parameterName, StringComparison.Ordinal))
            {
                return definition;
            }
        }

        return null;
    }

    private static IReadOnlyList<CompanyRuleParameterDefinition> BuildSurchargeParameters()
    {
        return new List<CompanyRuleParameterDefinition>
        {
            Time(CompanyRuleParameterNames.NightStart, "Start of the night-surcharge window (HH:mm)."),
            Time(CompanyRuleParameterNames.NightEnd, "End of the night-surcharge window (HH:mm)."),

            Rate(CompanyRuleParameterNames.NightRate, "Night surcharge rate."),
            Rate(CompanyRuleParameterNames.HolidayRate, "Public-holiday surcharge rate."),
            Rate(CompanyRuleParameterNames.Weekend1Rate, "First weekend-day surcharge rate."),
            Rate(CompanyRuleParameterNames.Weekend2Rate, "Second weekend-day surcharge rate."),
            Rate(CompanyRuleParameterNames.Weekend3Rate, "Third weekend-day surcharge rate."),

            EnumParam(CompanyRuleParameterNames.NightRateMode, "How the night rate is interpreted.", RateModeValues),
            EnumParam(CompanyRuleParameterNames.HolidayRateMode, "How the holiday rate is interpreted.", RateModeValues),
            EnumParam(CompanyRuleParameterNames.Weekend1RateMode, "How the first weekend rate is interpreted.", RateModeValues),
            EnumParam(CompanyRuleParameterNames.Weekend2RateMode, "How the second weekend rate is interpreted.", RateModeValues),
            EnumParam(CompanyRuleParameterNames.Weekend3RateMode, "How the third weekend rate is interpreted.", RateModeValues),

            Rate(CompanyRuleParameterNames.NightMinimumPerHour, "Minimum night surcharge per hour."),
            Rate(CompanyRuleParameterNames.HolidayMinimumPerHour, "Minimum holiday surcharge per hour."),
            Rate(CompanyRuleParameterNames.Weekend1MinimumPerHour, "Minimum first weekend surcharge per hour."),
            Rate(CompanyRuleParameterNames.Weekend2MinimumPerHour, "Minimum second weekend surcharge per hour."),
            Rate(CompanyRuleParameterNames.Weekend3MinimumPerHour, "Minimum third weekend surcharge per hour."),

            EnumParam(CompanyRuleParameterNames.StackingMode, "How overlapping surcharges combine.", StackingModeValues),

            EnumParam(CompanyRuleParameterNames.OvertimeBasis, "Period over which overtime hours accumulate.", OvertimeBasisValues),
            EnumParam(CompanyRuleParameterNames.OvertimeRateMode, "How overtime rates are interpreted.", OvertimeRateModeValues),
            Rate(CompanyRuleParameterNames.OvertimeTier1AfterHours, "Hours after which the first overtime tier applies."),
            Rate(CompanyRuleParameterNames.OvertimeTier1Rate, "First overtime-tier rate."),
            Rate(CompanyRuleParameterNames.OvertimeTier2AfterHours, "Hours after which the second overtime tier applies."),
            Rate(CompanyRuleParameterNames.OvertimeTier2Rate, "Second overtime-tier rate."),
            Rate(CompanyRuleParameterNames.OvertimeTier3AfterHours, "Hours after which the third overtime tier applies."),
            Rate(CompanyRuleParameterNames.OvertimeTier3Rate, "Third overtime-tier rate.")
        };
    }

    private static IReadOnlyList<CompanyRuleParameterDefinition> BuildCounterRuleParameters()
    {
        return new List<CompanyRuleParameterDefinition>
        {
            EnumParam(CompanyRuleParameterNames.EventType, "The countable per-person event.", EventTypeValues, required: true),
            EnumParam(CompanyRuleParameterNames.Period, "The calendar period the count is anchored to.", PeriodValues, required: true),
            new CompanyRuleParameterDefinition
            {
                Name = CompanyRuleParameterNames.Threshold,
                Description = "The count at which the rule triggers.",
                DataType = CompanyRuleParameterDataType.Integer,
                Required = true,
                Min = MinThreshold
            },
            EnumParam(CompanyRuleParameterNames.Enforcement, "Whether a violation warns or blocks the save.", EnforcementValues, required: true),
            new CompanyRuleParameterDefinition
            {
                Name = CompanyRuleParameterNames.HoursThreshold,
                Description = "Hours a shift must exceed to be counted; required only when eventType is ShiftExceedingHours.",
                DataType = CompanyRuleParameterDataType.Decimal,
                Required = false,
                Min = ZeroBound
            },
            Text(CompanyRuleParameterNames.SchedulingRuleName, "Optional scheduling-rule name that scopes the rule to one industry."),
            Text(CompanyRuleParameterNames.CounterRuleName, "Optional display name for the rule.")
        };
    }

    private static IReadOnlyList<CompanyRuleParameterDefinition> BuildCustomMacroParameters()
    {
        return new List<CompanyRuleParameterDefinition>
        {
            RequiredText(CompanyRuleParameterNames.MacroName, "The macro name."),
            RequiredText(CompanyRuleParameterNames.MacroScript, "The macro DSL script."),
            Text(CompanyRuleParameterNames.MacroDescription, "Optional description of the macro."),
            EnumParam(CompanyRuleParameterNames.MacroCategory, "The macro category (defaults to Shift).", MacroCategoryValues)
        };
    }

    private static CompanyRuleParameterDefinition Time(string name, string description) => new()
    {
        Name = name,
        Description = description,
        DataType = CompanyRuleParameterDataType.Time,
        Required = false
    };

    private static CompanyRuleParameterDefinition Rate(string name, string description) => new()
    {
        Name = name,
        Description = description,
        DataType = CompanyRuleParameterDataType.Decimal,
        Required = false,
        Min = ZeroBound
    };

    private static CompanyRuleParameterDefinition Text(string name, string description) => new()
    {
        Name = name,
        Description = description,
        DataType = CompanyRuleParameterDataType.Text,
        Required = false
    };

    private static CompanyRuleParameterDefinition RequiredText(string name, string description) => new()
    {
        Name = name,
        Description = description,
        DataType = CompanyRuleParameterDataType.Text,
        Required = true
    };

    private static CompanyRuleParameterDefinition EnumParam(string name, string description, IReadOnlyList<string> values, bool required = false) => new()
    {
        Name = name,
        Description = description,
        DataType = CompanyRuleParameterDataType.Enum,
        Required = required,
        EnumValues = values
    };
}
