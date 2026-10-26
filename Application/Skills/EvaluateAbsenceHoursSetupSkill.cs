// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises on an employee's absence-hours setup: runs the real calculation for a day, then derives deterministic
/// findings (no contract, contract ignored by the payment interval, 0 guaranteed hours by design, percent field
/// contradicting the hours ratio, scheduling rule overriding the contract, desired hours per day) and names the
/// place to change - preferably the contract. Read-only advice: nothing is changed; changing the contract or
/// assigning one is a separate step the system confirms once (update_contract and assign_contract_by_name are Sensitive).
/// </summary>
/// <param name="firstName">First name of the employee</param>
/// <param name="lastName">Last name of the employee</param>
/// <param name="idNumber">Optional client number to tell apart people with the same name</param>
/// <param name="date">Optional calendar day (yyyy-MM-dd); default: today</param>
/// <param name="absenceName">Optional absence type by name; default: the type booked on that day, else vacation</param>
/// <param name="desiredHoursPerDay">Optional hours an absence day should credit</param>
/// <param name="paymentInterval">Optional payment interval of the schedule view; default: the company setting</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Breaks;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateAbsenceHoursSetupSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_absence_hours_setup";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IAbsenceHoursCalculationService _calculationService;
    private readonly ICompanyClock _companyClock;

    public EvaluateAbsenceHoursSetupSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IAbsenceHoursCalculationService calculationService,
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
        var rawDate = GetParameter<string>(parameters, AbsenceHoursSkillParameters.Date);
        DateOnly date;
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            date = await _companyClock.GetTodayDateAsync(cancellationToken);
        }
        else if (!SkillCalendarStringParser.TryParseDateOnly(rawDate, context.UserLanguage, out date))
        {
            return SkillResult.Error($"Invalid date: {rawDate}. Expected yyyy-MM-dd.");
        }

        if (!ContractSkillInput.TryReadDecimal(parameters, AbsenceHoursSkillParameters.DesiredHoursPerDay, out var desiredHours, out var readError))
        {
            return SkillResult.Error(readError!);
        }

        if (desiredHours is < decimal.Zero)
        {
            return SkillResult.Error($"Parameter '{AbsenceHoursSkillParameters.DesiredHoursPerDay}' must not be negative.");
        }

        if (!AbsenceHoursSkillParameters.TryReadPaymentInterval(parameters, out var paymentInterval, out var intervalError))
        {
            return SkillResult.Error(intervalError!);
        }

        var firstName = GetParameter<string>(parameters, AbsenceHoursSkillParameters.FirstName);
        var lastName = GetParameter<string>(parameters, AbsenceHoursSkillParameters.LastName);
        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _clientSearchRepository,
            _clientRepository,
            firstName,
            lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName),
            cancellationToken,
            policy: NameMatchPolicy.Lenient);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(error ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var (absence, absenceError) = await _calculationService.ResolveAbsenceAsync(
            GetParameter<string>(parameters, AbsenceHoursSkillParameters.AbsenceName), client.Id, date, cancellationToken);
        if (absence == null)
        {
            return SkillResult.Error(absenceError!);
        }

        var calculation = await _calculationService.CalculateAsync(client.Id, date, absence, paymentInterval, cancellationToken);
        var clientName = $"{client.FirstName} {client.Name}".Trim();
        var findings = AbsenceHoursSetupAdvisor.Analyse(calculation, desiredHours);

        var evaluation = new AbsenceHoursSetupEvaluation(
            AbsenceHoursCalculationViewBuilder.Build(calculation, clientName, context.UserLanguage),
            desiredHours,
            findings,
            AbsenceHoursSetupAdvisor.BuildRecommendation(findings, calculation));

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }
}
