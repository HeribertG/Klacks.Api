// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static, database-free source of truth for the parameters the planning-profile setup collects. Every
/// parameter carries both a plain-language meaning and a plain-language planning impact so the assistant
/// can explain each answer's consequence. BaseIndustry is the single required parameter (the first
/// question); the remaining fields are optional overrides applied to the created scheduling rules, with
/// the "at least one override when starting from scratch" rule enforced by the validator.
/// </summary>

using System;
using System.Collections.Generic;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class PlanningProfileParameterCatalog : IPlanningProfileParameterCatalog
{
    private const decimal ZeroBound = 0m;
    private const decimal MaxHoursPerDay = 24m;
    private const decimal MaxHoursPerWeek = 168m;
    private const decimal MaxPauseHours = 48m;
    private const int MinConsecutiveDays = 1;
    private const int MaxConsecutiveDaysBound = 31;
    private const int MaxRestDaysBound = 7;
    private const int MaxVacationDaysBound = 365;

    private static readonly IReadOnlyList<string> OvertimeBasisValues = Enum.GetNames(typeof(OvertimeBasis));

    private static readonly IReadOnlyList<PlanningProfileParameterDefinition> Definitions = Build();

    public IReadOnlyList<PlanningProfileParameterDefinition> GetParameters() => Definitions;

    public PlanningProfileParameterDefinition? FindParameter(string parameterName)
    {
        foreach (var definition in Definitions)
        {
            if (string.Equals(definition.Name, parameterName, StringComparison.Ordinal))
            {
                return definition;
            }
        }

        return null;
    }

    private static IReadOnlyList<PlanningProfileParameterDefinition> Build()
    {
        return new List<PlanningProfileParameterDefinition>
        {
            new()
            {
                Name = PlanningProfileParameterNames.BaseIndustry,
                Description = "Which shipped industry profile is copied as your editable starting point, or 'scratch' to begin with a single blank rule.",
                PlanningImpact = "Choosing an industry copies all of its scheduling-rule templates into your own editable rules; 'scratch' creates one empty rule filled entirely from the answers that follow.",
                DataType = PlanningProfileParameterDataType.Enum,
                Required = true,
                EnumValues = PlanningProfileBaseChoices.All
            },
            new()
            {
                Name = PlanningProfileParameterNames.ProfileName,
                Description = "Display name for the single rule created when you start from scratch.",
                PlanningImpact = "Used only when baseIndustry is 'scratch'; when copying an industry the copied rules keep their template names.",
                DataType = PlanningProfileParameterDataType.Text,
                Required = false
            },
            Decimal(PlanningProfileParameterNames.DefaultWorkingHours,
                "The standard length of a normal working day in hours.",
                "Sets the baseline a shift is measured against, so under- and over-time are computed relative to it.",
                ZeroBound, MaxHoursPerDay),
            Decimal(PlanningProfileParameterNames.FullTimeHours,
                "Weekly hours that count as a 100% (full-time) workload.",
                "Scales part-time percentages and target-hour calculations for every employee on this rule.",
                ZeroBound, MaxHoursPerWeek),
            Decimal(PlanningProfileParameterNames.MaxDailyHours,
                "The maximum hours a person may be scheduled on a single day.",
                "The planner refuses to place a shift that would push a person's daily hours above this cap.",
                ZeroBound, MaxHoursPerDay),
            Decimal(PlanningProfileParameterNames.MaxWeeklyHours,
                "The maximum hours a person may be scheduled within one week.",
                "Caps total weekly assignment per person, protecting against overload across several shifts.",
                ZeroBound, MaxHoursPerWeek),
            Integer(PlanningProfileParameterNames.MaxConsecutiveDays,
                "The largest number of working days allowed in an unbroken run.",
                "After this many days in a row the planner forces a rest day before the next shift.",
                MinConsecutiveDays, MaxConsecutiveDaysBound),
            Decimal(PlanningProfileParameterNames.MinRestDays,
                "The minimum number of rest days required inside the planning window. Some jurisdictions " +
                "require a fractional weekly average (e.g. Spain: 1.5 days/week).",
                "Guarantees recovery time; the planner reserves at least this many days off per window.",
                ZeroBound, MaxRestDaysBound),
            Decimal(PlanningProfileParameterNames.MinPauseHours,
                "The minimum gap in hours between the end of one shift and the start of the next for the same person.",
                "Blocks back-to-back shifts that would leave too little rest between them.",
                ZeroBound, MaxPauseHours),
            Decimal(PlanningProfileParameterNames.OvertimeThreshold,
                "The hours beyond which worked time is treated as overtime.",
                "Hours above this threshold are flagged as overtime and can attract overtime rates.",
                ZeroBound, MaxHoursPerWeek),
            new()
            {
                Name = PlanningProfileParameterNames.OvertimeBasis,
                Description = "Whether overtime is accumulated per day or per week.",
                PlanningImpact = "Day resets the overtime count every calendar day; Week cumulates across the configured week before comparing to the threshold.",
                DataType = PlanningProfileParameterDataType.Enum,
                Required = false,
                EnumValues = OvertimeBasisValues
            },
            Time(PlanningProfileParameterNames.NightStart,
                "The clock time at which the night-surcharge window begins (HH:mm).",
                "Hours worked from this time onward qualify for the night surcharge."),
            Time(PlanningProfileParameterNames.NightEnd,
                "The clock time at which the night-surcharge window ends (HH:mm).",
                "Together with nightStart it bounds the window in which the night rate applies."),
            Rate(PlanningProfileParameterNames.NightRate,
                "The surcharge rate applied to hours inside the night window.",
                "Raises the cost of night hours; higher values make night shifts more expensive to schedule."),
            Rate(PlanningProfileParameterNames.HolidayRate,
                "The surcharge rate applied to work on public holidays.",
                "Raises the cost of holiday hours the same way the night rate does for night hours."),
            Integer(PlanningProfileParameterNames.VacationDaysPerYear,
                "The number of paid vacation days granted per year under this rule.",
                "Feeds vacation-entitlement and remaining-days calculations for employees on this rule.",
                (int)ZeroBound, MaxVacationDaysBound)
        };
    }

    private static PlanningProfileParameterDefinition Decimal(string name, string description, string impact, decimal min, decimal max) => new()
    {
        Name = name,
        Description = description,
        PlanningImpact = impact,
        DataType = PlanningProfileParameterDataType.Decimal,
        Required = false,
        Min = min,
        Max = max
    };

    private static PlanningProfileParameterDefinition Rate(string name, string description, string impact) => new()
    {
        Name = name,
        Description = description,
        PlanningImpact = impact,
        DataType = PlanningProfileParameterDataType.Decimal,
        Required = false,
        Min = ZeroBound
    };

    private static PlanningProfileParameterDefinition Integer(string name, string description, string impact, int min, int max) => new()
    {
        Name = name,
        Description = description,
        PlanningImpact = impact,
        DataType = PlanningProfileParameterDataType.Integer,
        Required = false,
        Min = min,
        Max = max
    };

    private static PlanningProfileParameterDefinition Time(string name, string description, string impact) => new()
    {
        Name = name,
        Description = description,
        PlanningImpact = impact,
        DataType = PlanningProfileParameterDataType.Time,
        Required = false
    };
}
