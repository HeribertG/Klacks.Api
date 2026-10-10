// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the starting point of a contract created from a template and reports afterwards which fields were
/// adapted and which were copied unchanged. CopyAsNew carries every value of the template (hours, all five
/// time-credit rates including WE3, night window, calendar, working weekdays, shift-work flag, scheduling rule,
/// individual pay period, payment interval, percent) into a fresh resource with no identity: no id, no
/// calendar navigation object and no validUntil, so the new contract is a separate row that is open-ended
/// until the administrator says otherwise. RuleOverridableFields names the fields a scheduling rule can override
/// at the point of use (the effective-contract-data resolution lets a rule value win over the contract's own for
/// exactly these), so adapting one of them on a contract that keeps the rule may have no visible effect.
/// </summary>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractTemplateCopier
{
    private static readonly string[] CopyableFieldNames =
    [
        ContractFieldNames.GuaranteedHours,
        ContractFieldNames.Percent,
        ContractFieldNames.MinimumHours,
        ContractFieldNames.MaximumHours,
        ContractFieldNames.FullTime,
        ContractFieldNames.PaymentInterval,
        ContractFieldNames.PerformsShiftWork,
        ContractFieldNames.Workdays,
        ContractFieldNames.Region,
        ContractFieldNames.IndividualPeriod,
        ContractFieldNames.NightRate,
        ContractFieldNames.HolidayRate,
        ContractFieldNames.SaRate,
        ContractFieldNames.SoRate,
        ContractFieldNames.We3Rate,
        ContractFieldNames.NightWindow,
        ContractFieldNames.SchedulingRule
    ];

    public static readonly IReadOnlyList<string> RuleOverridableFields =
    [
        ContractFieldNames.GuaranteedHours,
        ContractFieldNames.MinimumHours,
        ContractFieldNames.MaximumHours,
        ContractFieldNames.FullTime,
        ContractFieldNames.Workdays
    ];

    public static ContractResource CopyAsNew(ContractResource template)
    {
        return new ContractResource
        {
            Id = Guid.Empty,
            Name = template.Name,
            GuaranteedHours = template.GuaranteedHours,
            MaximumHours = template.MaximumHours,
            MinimumHours = template.MinimumHours,
            FullTime = template.FullTime,
            NightRate = template.NightRate,
            HolidayRate = template.HolidayRate,
            WE1Rate = template.WE1Rate,
            WE2Rate = template.WE2Rate,
            WE3Rate = template.WE3Rate,
            NightStart = template.NightStart,
            NightEnd = template.NightEnd,
            PaymentInterval = template.PaymentInterval,
            Percent = template.Percent,
            ValidFrom = template.ValidFrom,
            ValidUntil = null,
            CalendarSelection = null,
            CalendarSelectionId = template.CalendarSelectionId,
            WorkOnMonday = template.WorkOnMonday,
            WorkOnTuesday = template.WorkOnTuesday,
            WorkOnWednesday = template.WorkOnWednesday,
            WorkOnThursday = template.WorkOnThursday,
            WorkOnFriday = template.WorkOnFriday,
            WorkOnSaturday = template.WorkOnSaturday,
            WorkOnSunday = template.WorkOnSunday,
            PerformsShiftWork = template.PerformsShiftWork,
            SchedulingRuleId = template.SchedulingRuleId,
            IndividualPeriodId = template.IndividualPeriodId
        };
    }

    public static IReadOnlyList<string> AdaptedFields(ContractResource template, ContractResource result)
    {
        var adapted = new List<string> { ContractFieldNames.Name, ContractFieldNames.ValidFrom };

        AddIf(adapted, ContractFieldNames.ValidUntil, template.ValidUntil != result.ValidUntil);
        AddIf(adapted, ContractFieldNames.GuaranteedHours, template.GuaranteedHours != result.GuaranteedHours);
        AddIf(adapted, ContractFieldNames.Percent, template.Percent != result.Percent);
        AddIf(adapted, ContractFieldNames.MinimumHours, template.MinimumHours != result.MinimumHours);
        AddIf(adapted, ContractFieldNames.MaximumHours, template.MaximumHours != result.MaximumHours);
        AddIf(adapted, ContractFieldNames.FullTime, template.FullTime != result.FullTime);
        AddIf(adapted, ContractFieldNames.PaymentInterval, template.PaymentInterval != result.PaymentInterval);
        AddIf(adapted, ContractFieldNames.PerformsShiftWork, template.PerformsShiftWork != result.PerformsShiftWork);
        AddIf(adapted, ContractFieldNames.Workdays, !ContractWorkdays.Of(template).SetEquals(ContractWorkdays.Of(result)));
        AddIf(adapted, ContractFieldNames.Region, template.CalendarSelectionId != result.CalendarSelectionId);
        AddIf(adapted, ContractFieldNames.IndividualPeriod, template.IndividualPeriodId != result.IndividualPeriodId);

        return adapted;
    }

    public static IReadOnlyList<string> CopiedFields(IReadOnlyList<string> adaptedFields) =>
        CopyableFieldNames.Where(field => !adaptedFields.Contains(field)).ToList();

    private static void AddIf(List<string> fields, string field, bool differs)
    {
        if (differs)
        {
            fields.Add(field);
        }
    }
}
