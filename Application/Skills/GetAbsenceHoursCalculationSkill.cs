// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lays open how many hours an absence day of one employee credits and why: the real pipeline (effective contract
/// data, macro input, the macro of the absence type) runs on a transient entry, and every figure is reported with
/// the place it was taken from - contract, scheduling rule, company settings or monthly target hours row. Read-only;
/// nothing is booked or changed. A person hidden by group visibility is answered like an unknown one.
/// </summary>
/// <param name="firstName">First name of the employee</param>
/// <param name="lastName">Last name of the employee</param>
/// <param name="idNumber">Optional client number to tell apart people with the same name</param>
/// <param name="date">The calendar day of the absence (yyyy-MM-dd)</param>
/// <param name="absenceName">Optional absence type by name; default: the type booked on that day, else vacation</param>
/// <param name="paymentInterval">Optional payment interval of the schedule view; default: the company setting</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Breaks;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class GetAbsenceHoursCalculationSkill : BaseSkillImplementation
{
    public const string SkillName = "get_absence_hours_calculation";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IAbsenceHoursCalculationService _calculationService;

    public GetAbsenceHoursCalculationSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IAbsenceHoursCalculationService calculationService)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _calculationService = calculationService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rawDate = GetRequiredString(parameters, AbsenceHoursSkillParameters.Date);
        if (!SkillCalendarStringParser.TryParseDateOnly(rawDate, context.UserLanguage, out var date))
        {
            return SkillResult.Error($"Invalid date: {rawDate}. Expected yyyy-MM-dd.");
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
        var view = AbsenceHoursCalculationViewBuilder.Build(calculation, clientName, context.UserLanguage);

        return SkillResult.SuccessResult(view, BuildMessage(view));
    }

    private static string BuildMessage(AbsenceHoursCalculationView view)
    {
        var result = view.ResultHours is { } hours ? $"{hours} h" : "no value";
        var basis = view.HasActiveContract
            ? $"contract '{SkillMessageText.Name(view.ContractUsed)}'"
            : "no contract (company settings)";
        var ignored = view.ContractsIgnoredByPaymentInterval.Count > 0
            ? $" Ignored because of the payment interval {view.PaymentIntervalUsed}: {string.Join(", ", view.ContractsIgnoredByPaymentInterval)}."
            : string.Empty;

        return $"{SkillMessageText.Name(view.Employee)}, {view.Date}, {SkillMessageText.Name(view.AbsenceType)}: {result}. " +
               $"Daily hours {view.DailyHours} ({view.DailyHoursSource}) x workload {view.WorkloadPercent} % ({view.WorkloadPercentSource}" +
               $"{(view.WorkloadPercentFormula == null ? string.Empty : $": {view.WorkloadPercentFormula}")}); " +
               $"guaranteed hours {view.GuaranteedHours} ({view.GuaranteedHoursSource}); basis {basis}; " +
               $"weekend {view.IsWeekendDay}, holiday {view.IsHoliday}; rule applied: {view.ResultFormula}.{ignored} " +
               "Explain the sources in plain words (contract, scheduling rule, company settings, monthly target hours), never the enum names. " +
               "Hours are time credits, never money.";
    }
}
