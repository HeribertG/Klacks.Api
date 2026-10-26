// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lays open how the target hours of one employee for a month (pay period) are resolved: the way (no contract, fixed
/// hours, Weg 1, Weg 2), every input with its source as the resolver reports it, the contract that won and the ones a
/// schedule view interval would not see, the monthly row used or missing, the workload percent and the prorated period
/// target - on the same pipeline the schedule row header uses. Read-only; a person hidden by group visibility is
/// answered like an unknown one.
/// </summary>
/// <param name="firstName">First name of the employee</param>
/// <param name="lastName">Last name of the employee</param>
/// <param name="idNumber">Optional client number to tell apart people with the same name</param>
/// <param name="month">Month to resolve as yyyy-MM or any date in it; default: the current month</param>
/// <param name="paymentInterval">Optional payment interval of the schedule view; default: the company setting</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class GetTargetHoursCalculationSkill : BaseSkillImplementation
{
    public const string SkillName = "get_target_hours_calculation";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ITargetHoursCalculationService _calculationService;
    private readonly ICompanyClock _companyClock;

    public GetTargetHoursCalculationSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ITargetHoursCalculationService calculationService,
        ICompanyClock companyClock)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _calculationService = calculationService;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (monthOk, dayInPeriod, monthError) = await TargetHoursSkillParameters.ReadMonthAsync(parameters, context.UserLanguage, _companyClock, cancellationToken);
        if (!monthOk)
        {
            return SkillResult.Error(monthError!);
        }

        if (!AbsenceHoursSkillParameters.TryReadPaymentInterval(parameters, out var paymentInterval, out var intervalError))
        {
            return SkillResult.Error(intervalError!);
        }

        var firstName = GetParameter<string>(parameters, AbsenceHoursSkillParameters.FirstName);
        var lastName = GetParameter<string>(parameters, AbsenceHoursSkillParameters.LastName);
        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _clientSearchRepository, _clientRepository, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken, policy: NameMatchPolicy.Lenient);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(error ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var calculation = await _calculationService.CalculateAsync(client.Id, dayInPeriod, paymentInterval, cancellationToken);
        var view = TargetHoursCalculationViewBuilder.Build(calculation, $"{client.FirstName} {client.Name}".Trim());

        return SkillResult.SuccessResult(view, BuildMessage(view, calculation));
    }

    private static string BuildMessage(TargetHoursCalculationView view, TargetHoursCalculation calculation)
    {
        var filtered = view.ContractsFilteredByViewInterval.Count > 0
            ? $" Not seen by a schedule view on {view.ViewInterval}: {string.Join(", ", view.ContractsFilteredByViewInterval)}."
            : string.Empty;

        return $"{SkillMessageText.Name(view.Employee)}, {view.PeriodStart} to {view.PeriodEnd}: target {view.PeriodTargetHours} h " +
               $"({TargetHoursSetupAdvisor.DescribeWay(calculation.Way)}). Guaranteed hours: {view.GuaranteedHoursFormula} " +
               $"(source {view.GuaranteedHoursSource}); workload {view.WorkloadPercent} % ({view.WorkloadPercentSource}); " +
               $"contract {(view.ContractUsed == null ? "none" : $"'{SkillMessageText.Name(view.ContractUsed)}' ({view.ContractPaymentInterval})")}; " +
               $"monthly table: {view.MonthsWithRowInYear} of 12 months filled, this month {(view.MonthlyTargetHoursRow is { } row ? $"{row} h" : "no row")}.{filtered} " +
               "Explain the sources in plain words (contract, scheduling rule, company settings, monthly target hours), never the enum names. Hours are time, never money.";
    }
}
