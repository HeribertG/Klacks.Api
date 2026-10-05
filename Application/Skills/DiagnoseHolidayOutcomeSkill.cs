// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only diagnosis for one employee on one date: does a public holiday earn the holiday time surcharge, does it
/// raise the "work on a statutory holiday" warning, and which step decided it (contract of the day, holiday calendar
/// and where it comes from, official or reminder only, marked for the time surcharge, rate, exemption). Admins and
/// supervisors (granted contract editing) see rates, the warn/block reaction and the exemption; everybody else gets the verdicts and reasons
/// only. A person hidden by group visibility is answered like an unknown one.
/// </summary>
/// <param name="firstName">First name of the employee</param>
/// <param name="lastName">Last name of the employee</param>
/// <param name="idNumber">Optional client number to tell apart people with the same name</param>
/// <param name="date">The calendar day in question (yyyy-MM-dd)</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("diagnose_holiday_outcome")]
public class DiagnoseHolidayOutcomeSkill : BaseSkillImplementation
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    private const string StaleHint =
        "Works saved before a change of the contract calendar, the calendar entries or a holiday rule are not " +
        "recalculated automatically; if the stored surcharge of such a work differs from this verdict, that is the reason.";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IHolidayOutcomeDiagnosisService _diagnosisService;

    public DiagnoseHolidayOutcomeSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IHolidayOutcomeDiagnosisService diagnosisService)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _diagnosisService = diagnosisService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rawDate = GetRequiredString(parameters, "date");
        if (!SkillCalendarStringParser.TryParseDateOnly(rawDate, context.UserLanguage, out var date))
        {
            return SkillResult.Error($"Invalid date: {rawDate}. Expected yyyy-MM-dd.");
        }

        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _clientSearchRepository,
            _clientRepository,
            firstName,
            lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName),
            cancellationToken);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(error ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var clientName = $"{client.FirstName} {client.Name}".Trim();
        var diagnosis = await _diagnosisService.DiagnoseAsync(client.Id, clientName, date, cancellationToken);
        var showDetails = MaySeeRatesAndEnforcement(context.UserPermissions);

        var holidayName = diagnosis.HolidayName?.GetValueOrFirstAvailable(context.UserLanguage);
        var calendarName = HolidayCalendarDisplayName.Of(diagnosis.Calendar);

        var data = new
        {
            Employee = clientName,
            Date = date.ToString(DateFormat),
            HasActiveContract = diagnosis.HasActiveContract,
            Contract = diagnosis.ContractName,
            HolidayCalendar = calendarName,
            HolidayCalendarSource = diagnosis.Calendar.Source.ToString(),
            Holiday = holidayName,
            HolidayStatus = diagnosis.Status.ToString(),
            DowngradedByReminderOnly = diagnosis.DowngradedByReminderOnly,
            EarnsHolidayTimeSurcharge = diagnosis.EarnsHolidayTimeSurcharge,
            SurchargeReason = diagnosis.SurchargeReason,
            HolidayWorkWarningRaised = diagnosis.HolidayWorkWarningRaised,
            WarningReason = diagnosis.WarningReason,
            HolidayRate = showDetails ? diagnosis.HolidayRate : (decimal?)null,
            EnforcementMode = showDetails ? diagnosis.EnforcementMode.ToString() : null,
            Exemption = showDetails ? diagnosis.ExemptionDescription : null,
            ExemptionBoundToSchedulingRule = showDetails ? diagnosis.ExemptionSchedulingRuleName : null,
            WorksTouchingTheDate = diagnosis.WorksOnDate.Select(work => new
            {
                WorkDate = work.WorkDate.ToString(DateFormat),
                From = work.StartTime.ToString(TimeFormat),
                Until = work.EndTime.ToString(TimeFormat),
                work.StartsTheDayBefore,
            }).ToList(),
            StaleHint,
        };

        return SkillResult.SuccessResult(data, BuildMessage(clientName, date, diagnosis, holidayName, calendarName, showDetails));
    }

    private static bool MaySeeRatesAndEnforcement(IReadOnlyList<string> permissions) =>
        Permissions.HasAnyPermission(permissions, Permissions.CanEditContracts);

    private static string BuildMessage(
        string clientName,
        DateOnly date,
        HolidayOutcomeDiagnosis diagnosis,
        string? holidayName,
        string? calendarName,
        bool showDetails)
    {
        var day = date.ToString(DateFormat);
        var calendar = calendarName == null
            ? "no holiday calendar applies"
            : $"holiday calendar '{SkillMessageText.Name(calendarName)}' ({diagnosis.Calendar.Source})";
        var holiday = holidayName == null ? "no holiday" : $"'{SkillMessageText.Name(holidayName)}' ({diagnosis.Status})";
        var works = diagnosis.WorksOnDate.Count == 0 ? "no work touches this day" : $"{diagnosis.WorksOnDate.Count} work(s) touch this day";

        var message = $"{SkillMessageText.Name(clientName)}, {day}: {calendar}; {holiday}; {works}. " +
                      $"Holiday time surcharge: {(diagnosis.EarnsHolidayTimeSurcharge && diagnosis.HolidayRate != 0m ? "yes" : "no")} " +
                      $"(reason {diagnosis.SurchargeReason}). " +
                      $"Holiday-work warning: {(diagnosis.HolidayWorkWarningRaised ? "yes" : "no")} (reason {diagnosis.WarningReason}).";

        if (showDetails)
        {
            message += $" Holiday rate {diagnosis.HolidayRate}, reaction {diagnosis.EnforcementMode}.";

            if (diagnosis.SurchargeReason == HolidayOutcomeReasonCodes.Applies)
            {
                message += " Another uplift (night, Saturday, Sunday) with a higher rate can win over the holiday rate when only the highest one is paid.";
            }
        }

        return message + " Relay the reasons in plain words, never the reason codes.";
    }
}
