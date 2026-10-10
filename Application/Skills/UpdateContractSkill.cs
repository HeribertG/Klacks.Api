// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a contract template (master data). Only fields supplied as parameters are changed;
/// hour values must not be negative and minimumHours must not exceed maximumHours. This skill
/// does NOT assign a contract to an employee — use assign_contract_by_name or
/// assign_contract_to_client for that.
/// </summary>
/// <param name="contractId">Required. Contract id (UUID) or exact contract name of the contract to update.</param>
/// <param name="name">Optional. New contract name.</param>
/// <param name="guaranteedHours">Optional. New guaranteed hours.</param>
/// <param name="clearGuaranteedHours">Optional. If true, clears the guaranteed hours so the contract inherits the company-wide value (monthly target hours or settings default), scaled by percent.</param>
/// <param name="percent">Optional. Workload share in percent; scales the inherited company value and feeds absence macros.</param>
/// <param name="clearPercent">Optional. If true, removes the percent (treated as full workload).</param>
/// <param name="minimumHours">Optional. New minimum hours.</param>
/// <param name="maximumHours">Optional. New maximum hours.</param>
/// <param name="fullTime">Optional. New full-time reference hours.</param>
/// <param name="nightRate">Optional. New night time-credit factor (0.1 = 6 minutes per hour); 0 means explicitly no credit.</param>
/// <param name="clearNightRate">Optional. If true, resets the night rate to the standard (scheduling rule, then installation settings).</param>
/// <param name="holidayRate">Optional. New holiday time-credit factor; 0 means explicitly no credit.</param>
/// <param name="clearHolidayRate">Optional. If true, resets the holiday rate to the standard.</param>
/// <param name="saRate">Optional. New Saturday time-credit factor; 0 means explicitly no credit.</param>
/// <param name="clearSaRate">Optional. If true, resets the Saturday rate to the standard.</param>
/// <param name="soRate">Optional. New Sunday time-credit factor; 0 means explicitly no credit.</param>
/// <param name="clearSoRate">Optional. If true, resets the Sunday rate to the standard.</param>
/// <param name="performsShiftWork">Optional. Whether the employee works late/night shifts. When false, the contract's own rates are ignored and the standard rates apply.</param>
/// <param name="clearPerformsShiftWork">Optional. If true, resets the shift-work flag to the standard (scheduling rule, then installation default).</param>
/// <param name="validFrom">Optional. New validity start date (YYYY-MM-DD).</param>
/// <param name="validUntil">Optional. New validity end date (YYYY-MM-DD).</param>
/// <param name="clearValidUntil">Optional. If true, removes the validity end date.</param>
/// <param name="workdays">Optional. Working weekdays as a comma separated list of Mon,Tue,Wed,Thu,Fri,Sat,Sun; listed days are worked, all others are not.</param>
/// <param name="region">Optional. State, canton or region code that selects the holiday calendar; must match exactly one calendar unless the contract's current calendar is among several matches.</param>
/// <param name="clearRegion">Optional. If true, removes the contract's own holiday calendar so the company-wide calendar applies.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_contract")]
public class UpdateContractSkill : BaseSkillImplementation
{
    private const string ContractCalendarOwner = "contract's";

    private const string SchedulingRuleWorkdaysWarning =
        "The contract has a scheduling rule that may override the working weekdays: a value the rule sets wins " +
        "over the contract's own value, so check the rule in the contract settings page.";

    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ICountryResolver _countryResolver;
    private readonly IContractRepository _contractRepository;

    public UpdateContractSkill(
        IMediator mediator,
        ICompanyClock companyClock,
        ICalendarSelectionRepository calendarSelectionRepository,
        ICountryResolver countryResolver,
        IContractRepository contractRepository)
    {
        _mediator = mediator;
        _companyClock = companyClock;
        _calendarSelectionRepository = calendarSelectionRepository;
        _countryResolver = countryResolver;
        _contractRepository = contractRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (contract, loadError) = await LoadContractAsync(parameters, cancellationToken);
        if (contract == null)
        {
            return SkillResult.Error(loadError!);
        }

        var contractId = contract.Id;
        var resolvedName = contract.Name;

        var changed = new List<string>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != contract.Name)
        {
            contract.Name = name.Trim();
            changed.Add("name");
        }

        var decimalFields = new (string Key, Func<decimal> Get, Action<decimal> Set)[]
        {
            ("minimumHours", () => contract.MinimumHours, v => contract.MinimumHours = v),
            ("maximumHours", () => contract.MaximumHours, v => contract.MaximumHours = v),
            ("fullTime", () => contract.FullTime, v => contract.FullTime = v),
        };

        foreach (var field in decimalFields)
        {
            var value = GetParameter<decimal?>(parameters, field.Key);
            if (!value.HasValue)
            {
                continue;
            }

            if (value.Value < decimal.Zero)
            {
                return SkillResult.Error($"Parameter '{field.Key}' must not be negative.");
            }

            if (value.Value == field.Get())
            {
                continue;
            }

            field.Set(value.Value);
            changed.Add(field.Key);
        }

        var nullableDecimalError = ApplyNullableDecimalFields(parameters, contract, changed);
        if (nullableDecimalError != null)
        {
            return SkillResult.Error(nullableDecimalError);
        }

        var standardResetError = ApplyShiftWorkAndStandardResets(parameters, contract, changed);
        if (standardResetError != null)
        {
            return SkillResult.Error(standardResetError);
        }

        var workdaysRegionError = await ApplyWorkdaysAndRegionAsync(parameters, contract, changed, cancellationToken);
        if (workdaysRegionError != null)
        {
            return SkillResult.Error(workdaysRegionError);
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var (validFrom, invalidValidFrom) = SkillDateParser.ParseOptionalUtcDate(validFromStr, today, context.UserLanguage);
        if (invalidValidFrom)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validFrom", validFromStr!));
        }

        if (validFrom.HasValue && validFrom.Value != contract.ValidFrom)
        {
            contract.ValidFrom = validFrom.Value;
            changed.Add("validFrom");
        }

        var clearGuaranteedHours = GetParameter<bool>(parameters, "clearGuaranteedHours", false);
        if (clearGuaranteedHours && contract.GuaranteedHours != null)
        {
            contract.GuaranteedHours = null;
            changed.Add("guaranteedHours");
        }

        var clearPercent = GetParameter<bool>(parameters, "clearPercent", false);
        if (clearPercent && contract.Percent != null)
        {
            contract.Percent = null;
            changed.Add("percent");
        }

        var validUntilError = ApplyValidUntil(parameters, contract, changed, today, context.UserLanguage);
        if (validUntilError != null)
        {
            return SkillResult.Error(validUntilError);
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ContractId = contractId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — contract left unchanged.");
        }

        if (contract.MinimumHours > contract.MaximumHours && contract.MaximumHours > decimal.Zero)
        {
            return SkillResult.Error("minimumHours must not exceed maximumHours.");
        }

        if (contract.ValidUntil.HasValue && contract.ValidUntil.Value < contract.ValidFrom)
        {
            return SkillResult.Error("validUntil must not be before validFrom.");
        }

        var updated = await _mediator.Send(new PutCommand<ContractResource>(contract), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating contract '{contractId}' failed.");
        }

        var warnings = new List<string>();
        if (changed.Contains(ContractFieldNames.Workdays) && updated.SchedulingRuleId.HasValue)
        {
            warnings.Add(SchedulingRuleWorkdaysWarning);
        }

        return SkillResult.SuccessResult(
            new
            {
                ContractId = contractId,
                ChangedFields = changed,
                updated.Name,
                updated.GuaranteedHours,
                updated.Percent,
                updated.MinimumHours,
                updated.MaximumHours,
                updated.ValidFrom,
                updated.ValidUntil,
                Workdays = ContractWorkdays.Format(ContractWorkdays.Of(updated)),
                updated.CalendarSelectionId,
                Warnings = warnings
            },
            $"Contract '{resolvedName}' (id {contractId}) updated ({string.Join(", ", changed)})." +
            (warnings.Count > 0 ? " Warning: " + string.Join(" ", warnings) : string.Empty));
    }

    private async Task<(ContractResource? Contract, string? Error)> LoadContractAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var (contractId, referenceError) = await ContractReferenceResolver.ResolveIdAsync(
            _contractRepository, GetParameter<string>(parameters, ContractFieldNames.ContractId),
            ContractFieldNames.ContractId, ContractNameMatchMode.ExactOrUniquePartial, cancellationToken);
        if (contractId == null)
        {
            return (null, referenceError);
        }

        try
        {
            return (await _mediator.Send(new GetQuery<ContractResource>(contractId.Value), cancellationToken), null);
        }
        catch (KeyNotFoundException)
        {
            return (null, $"Contract '{contractId}' not found.");
        }
    }

    private string? ApplyValidUntil(
        Dictionary<string, object> parameters, ContractResource contract, List<string> changed, DateTime today, string? language)
    {
        var clearValidUntil = GetParameter<bool>(parameters, "clearValidUntil", false);
        if (clearValidUntil)
        {
            if (contract.ValidUntil != null)
            {
                contract.ValidUntil = null;
                changed.Add("validUntil");
            }

            return null;
        }

        var validUntilStr = GetParameter<string>(parameters, "validUntil");
        var (validUntil, invalidValidUntil) = SkillDateParser.ParseOptionalUtcDate(validUntilStr, today, language);
        if (invalidValidUntil)
        {
            return SkillDateParser.InvalidDateMessageFor("validUntil", validUntilStr!);
        }

        if (validUntil.HasValue && validUntil.Value != contract.ValidUntil)
        {
            contract.ValidUntil = validUntil.Value;
            changed.Add("validUntil");
        }

        return null;
    }

    private async Task<string?> ApplyWorkdaysAndRegionAsync(
        Dictionary<string, object> parameters, ContractResource contract, List<string> changed, CancellationToken cancellationToken)
    {
        return ApplyWorkdays(parameters, contract, changed)
            ?? await ApplyRegionAsync(parameters, contract, changed, cancellationToken);
    }

    private static string? ApplyWorkdays(
        Dictionary<string, object> parameters, ContractResource contract, List<string> changed)
    {
        var raw = GetParameter<string>(parameters, ContractFieldNames.Workdays);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!ContractWorkdays.TryParse(raw, out var days, out var error))
        {
            return error;
        }

        if (days.SetEquals(ContractWorkdays.Of(contract)))
        {
            return null;
        }

        ContractWorkdays.Apply(contract, days);
        changed.Add(ContractFieldNames.Workdays);
        return null;
    }

    // A null calendar selection is a real state, not "unknown": the holiday resolution then falls back to the
    // company-wide calendar, so clearRegion resets to that fallback. Supplying a region together with
    // clearRegion is contradictory and rejected.
    private async Task<string?> ApplyRegionAsync(
        Dictionary<string, object> parameters, ContractResource contract, List<string> changed, CancellationToken cancellationToken)
    {
        if (!ContractSkillInput.TryReadBool(parameters, ContractFieldNames.ClearRegion, out var clearRegion, out var readError))
        {
            return readError;
        }

        var region = GetParameter<string>(parameters, ContractFieldNames.Region)?.Trim();
        var regionSupplied = !string.IsNullOrEmpty(region);

        if (clearRegion == true)
        {
            if (regionSupplied)
            {
                return $"Parameter '{ContractFieldNames.Region}' must not be combined with '{ContractFieldNames.ClearRegion}'.";
            }

            SetCalendar(contract, null, changed);
            return null;
        }

        if (!regionSupplied)
        {
            return null;
        }

        var (calendarId, error) = await ContractRegionCalendarResolver.ChooseAsync(
            _countryResolver, _calendarSelectionRepository, region!, contract.CalendarSelectionId,
            ContractCalendarOwner, cancellationToken);

        if (error != null)
        {
            return error;
        }

        SetCalendar(contract, calendarId, changed);
        return null;
    }

    private static void SetCalendar(ContractResource contract, Guid? calendarId, List<string> changed)
    {
        if (contract.CalendarSelectionId == calendarId)
        {
            return;
        }

        contract.CalendarSelectionId = calendarId;
        changed.Add(ContractFieldNames.Region);
    }

    private string? ApplyNullableDecimalFields(
        Dictionary<string, object> parameters, ContractResource contract, List<string> changed)
    {
        var nullableDecimalFields = new (string Key, Func<decimal?> Get, Action<decimal?> Set)[]
        {
            ("guaranteedHours", () => contract.GuaranteedHours, v => contract.GuaranteedHours = v),
            ("percent", () => contract.Percent, v => contract.Percent = v),
            ("nightRate", () => contract.NightRate, v => contract.NightRate = v),
            ("holidayRate", () => contract.HolidayRate, v => contract.HolidayRate = v),
            ("saRate", () => contract.WE1Rate, v => contract.WE1Rate = v),
            ("soRate", () => contract.WE2Rate, v => contract.WE2Rate = v),
        };

        foreach (var field in nullableDecimalFields)
        {
            var value = GetParameter<decimal?>(parameters, field.Key);
            if (!value.HasValue)
            {
                continue;
            }

            if (value.Value < decimal.Zero)
            {
                return $"Parameter '{field.Key}' must not be negative.";
            }

            if (value.Value == field.Get())
            {
                continue;
            }

            field.Set(value.Value);
            changed.Add(field.Key);
        }

        return null;
    }

    // A clear* flag resets the value to null = "standard" (scheduling rule, then installation settings).
    // Supplying a value together with its clear* flag is contradictory and rejected.
    private string? ApplyShiftWorkAndStandardResets(
        Dictionary<string, object> parameters, ContractResource contract, List<string> changed)
    {
        var performsShiftWork = GetParameter<bool?>(parameters, "performsShiftWork");

        var clearToStandardFields = new (string ClearKey, string Key, bool Supplied, Func<bool> IsSet, Action Clear)[]
        {
            ("clearNightRate", "nightRate", GetParameter<decimal?>(parameters, "nightRate").HasValue, () => contract.NightRate != null, () => contract.NightRate = null),
            ("clearHolidayRate", "holidayRate", GetParameter<decimal?>(parameters, "holidayRate").HasValue, () => contract.HolidayRate != null, () => contract.HolidayRate = null),
            ("clearSaRate", "saRate", GetParameter<decimal?>(parameters, "saRate").HasValue, () => contract.WE1Rate != null, () => contract.WE1Rate = null),
            ("clearSoRate", "soRate", GetParameter<decimal?>(parameters, "soRate").HasValue, () => contract.WE2Rate != null, () => contract.WE2Rate = null),
            ("clearPerformsShiftWork", "performsShiftWork", performsShiftWork.HasValue, () => contract.PerformsShiftWork != null, () => contract.PerformsShiftWork = null),
        };

        var requestedClears = clearToStandardFields
            .Where(field => GetParameter<bool>(parameters, field.ClearKey, false))
            .ToList();

        var contradictory = requestedClears.FirstOrDefault(field => field.Supplied);
        if (contradictory.ClearKey != null)
        {
            return $"Parameter '{contradictory.Key}' must not be combined with '{contradictory.ClearKey}'.";
        }

        if (performsShiftWork.HasValue && performsShiftWork != contract.PerformsShiftWork)
        {
            contract.PerformsShiftWork = performsShiftWork;
            changed.Add("performsShiftWork");
        }

        foreach (var field in requestedClears.Where(field => field.IsSet()))
        {
            field.Clear();
            changed.Add(field.Key);
        }

        return null;
    }
}