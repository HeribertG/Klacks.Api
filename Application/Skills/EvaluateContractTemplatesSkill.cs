// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Compares the active contract templates with what the administrator wants a new contract to look like and
/// ranks the closest ones, so Klacksy can recommend a concrete template before anything is created. Read-only
/// advice: ranking, differences and the recommendation are computed in ContractTemplateComparer, never guessed.
/// Creating the contract from the chosen template stays a separate step: the administrator picks the template
/// and the system asks for the final confirmation before anything is written.
/// </summary>
/// <param name="paymentInterval">Optional. Wished payment interval: Weekly, Biweekly, Monthly, Individual or MonthlyTargetHours.</param>
/// <param name="guaranteedHours">Optional. Wished fixed guaranteed hours per interval (workload path 1).</param>
/// <param name="percent">Optional. Wished workload percent of the company-wide value (workload path 2).</param>
/// <param name="fullTime">Optional. Wished full-time reference hours.</param>
/// <param name="performsShiftWork">Optional. Whether the employee works late or night shifts.</param>
/// <param name="workdays">Optional. Wished working weekdays as a comma separated list of Mon,Tue,Wed,Thu,Fri,Sat,Sun.</param>
/// <param name="region">Optional. State, canton or region code of the wished holiday calendar (for example ZH).</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("evaluate_contract_templates")]
public class EvaluateContractTemplatesSkill : BaseSkillImplementation
{
    private readonly IContractRepository _contractRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ICountryResolver _countryResolver;
    private readonly ICompanyClock _companyClock;

    public EvaluateContractTemplatesSkill(
        IContractRepository contractRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        ICountryResolver countryResolver,
        ICompanyClock companyClock)
    {
        _contractRepository = contractRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _countryResolver = countryResolver;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.GuaranteedHours, out var guaranteedHours, out var readError)
            || !ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.Percent, out var percent, out readError)
            || !ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.FullTime, out var fullTime, out readError)
            || !ContractSkillInput.TryReadBool(parameters, ContractFieldNames.PerformsShiftWork, out var performsShiftWork, out readError))
        {
            return SkillResult.Error(readError!);
        }

        var negative = new (string Key, decimal? Value)[]
        {
            (ContractFieldNames.GuaranteedHours, guaranteedHours),
            (ContractFieldNames.Percent, percent),
            (ContractFieldNames.FullTime, fullTime)
        };
        foreach (var (key, value) in negative)
        {
            if (value is < decimal.Zero)
            {
                return SkillResult.Error($"Parameter '{key}' must not be negative.");
            }
        }

        PaymentInterval? paymentInterval = null;
        var paymentIntervalRaw = GetParameter<string>(parameters, ContractFieldNames.PaymentInterval);
        if (!string.IsNullOrWhiteSpace(paymentIntervalRaw))
        {
            if (!ContractPaymentIntervalParser.TryParse(paymentIntervalRaw, out var parsedInterval, out var intervalError))
            {
                return SkillResult.Error(intervalError!);
            }

            paymentInterval = parsedInterval;
        }

        IReadOnlySet<DayOfWeek>? workdays = null;
        var workdaysRaw = GetParameter<string>(parameters, ContractFieldNames.Workdays);
        if (!string.IsNullOrWhiteSpace(workdaysRaw))
        {
            if (!ContractWorkdays.TryParse(workdaysRaw, out var parsedDays, out var workdaysError))
            {
                return SkillResult.Error(workdaysError!);
            }

            workdays = parsedDays;
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var templates = (await _contractRepository.List())
            .Where(contract => contract.ValidUntil == null || contract.ValidUntil >= today)
            .ToList();

        var calendarSelections = await _calendarSelectionRepository.List();
        var calendarNames = calendarSelections.ToDictionary(selection => selection.Id, selection => selection.Name);

        var warnings = new List<string>();
        if (guaranteedHours.HasValue && percent.HasValue)
        {
            warnings.Add(
                $"'{ContractFieldNames.GuaranteedHours}' and '{ContractFieldNames.Percent}' are mutually exclusive workload " +
                "paths; ask the administrator which one applies before creating a contract.");
        }

        var region = await ResolveRegionAsync(parameters, calendarSelections.Select(s => s.Name), warnings, cancellationToken);

        var wish = new ContractTemplateWish(
            paymentInterval, guaranteedHours, percent, fullTime, performsShiftWork, workdays, region);

        var candidates = ContractTemplateComparer.Rank(
            templates, wish, calendarNames, ContractTemplateAdvisoryDefaults.MaxCandidates);

        var result = new ContractTemplateEvaluationResult(
            templates.Count,
            wish.WishCount,
            candidates,
            warnings,
            ContractTemplateComparer.BuildRecommendation(candidates, templates.Count, wish));

        return SkillResult.SuccessResult(result, result.Recommendation);
    }

    private async Task<ContractTemplateRegionWish?> ResolveRegionAsync(
        Dictionary<string, object> parameters,
        IEnumerable<string> calendarNames,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var regionRaw = GetParameter<string>(parameters, ContractFieldNames.Region);
        if (string.IsNullOrWhiteSpace(regionRaw))
        {
            return null;
        }

        var ids = await ContractRegionCalendarResolver.ResolveCalendarSelectionIdsAsync(
            _countryResolver, _calendarSelectionRepository, regionRaw, cancellationToken);

        if (ids.Count == 0)
        {
            var names = calendarNames.ToList();
            warnings.Add(
                $"No holiday calendar is configured for region '{regionRaw.Trim()}', so every template counts as a region " +
                "difference. Existing calendars: " + (names.Count > 0 ? string.Join(", ", names) : "none") + ".");
        }

        return new ContractTemplateRegionWish(regionRaw.Trim(), ids.ToHashSet());
    }
}
