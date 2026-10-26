// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises on the target-hours setup of an employee or, without a name, of the whole company: which way fits (no
/// contract, fixed hours, Weg 1, Weg 2), gaps and odd values in the monthly target hours table, a percent without
/// effect, contradictory contract fields, a contract a schedule view would not see, and a no-contract employee who
/// should not be at 100 %. Read-only advice computed in C#; changing a contract, the table or an assignment is a
/// separate step the system confirms once (all AdvisesFor targets are Sensitive).
/// </summary>
/// <param name="firstName">First name of the employee (omit both names for the company-wide view)</param>
/// <param name="lastName">Last name of the employee</param>
/// <param name="idNumber">Optional client number to tell apart people with the same name</param>
/// <param name="month">Month to evaluate as yyyy-MM or a date in it; default: the current month</param>
/// <param name="desiredWorkloadPercent">Optional workload share the employee should have</param>
/// <param name="paymentInterval">Optional payment interval of the schedule view; default: the company setting</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateTargetHoursSetupSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_target_hours_setup";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ITargetHoursCalculationService _calculationService;
    private readonly ICompanyClock _companyClock;

    public EvaluateTargetHoursSetupSkill(
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

        if (!ContractSkillInput.TryReadDecimal(parameters, TargetHoursSkillParameters.DesiredWorkloadPercent, out var desiredPercent, out var readError))
        {
            return SkillResult.Error(readError!);
        }

        if (desiredPercent is < decimal.Zero)
        {
            return SkillResult.Error($"Parameter '{TargetHoursSkillParameters.DesiredWorkloadPercent}' must not be negative.");
        }

        if (!AbsenceHoursSkillParameters.TryReadPaymentInterval(parameters, out var paymentInterval, out var intervalError))
        {
            return SkillResult.Error(intervalError!);
        }

        var firstName = GetParameter<string>(parameters, AbsenceHoursSkillParameters.FirstName);
        var lastName = GetParameter<string>(parameters, AbsenceHoursSkillParameters.LastName);
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return await EvaluateCompanyAsync(dayInPeriod.Year, cancellationToken);
        }

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _clientSearchRepository, _clientRepository, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken, policy: NameMatchPolicy.Lenient);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(error ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var calculation = await _calculationService.CalculateAsync(client.Id, dayInPeriod, paymentInterval, cancellationToken);
        var findings = TargetHoursSetupAdvisor.Analyse(
            calculation, calculation.MonthlyTargetHoursOfYear, calculation.WorkdaysPerMonth, calculation.Settings, desiredPercent);
        var evaluation = new TargetHoursSetupEvaluation(
            TargetHoursCalculationViewBuilder.Build(calculation, $"{client.FirstName} {client.Name}".Trim()),
            calculation.MonthlyTargetHoursOfYear,
            calculation.WorkdaysPerMonth,
            desiredPercent,
            findings,
            TargetHoursSetupAdvisor.BuildRecommendation(findings, calculation));

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }

    private async Task<SkillResult> EvaluateCompanyAsync(int year, CancellationToken cancellationToken)
    {
        var (rows, workdays, settings) = await _calculationService.LoadCompanyFactsAsync(year, cancellationToken);
        var findings = TargetHoursSetupAdvisor.Analyse(null, rows, workdays, settings, null);
        var evaluation = new TargetHoursSetupEvaluation(
            null, rows, workdays, null, findings, TargetHoursSetupAdvisor.BuildRecommendation(findings, null));

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }
}
