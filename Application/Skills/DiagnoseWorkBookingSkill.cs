// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only diagnosis of ONE booking: why can this employee not get this shift on this day right now, and what lifts it.
/// Resolves the employee (id, or name plus optional client number) and the shift (id, or name/abbreviation) through the
/// group-visibility guards, so a hidden employee or shift is answered exactly like an unknown one, then asks
/// IWorkBookingDiagnosisService, which runs the direct booking path's own checks in its order. Each reason comes with its
/// remedy and the entry paths it applies to (direct entry, Klacksy, schedule grid), worded as English sentences the
/// model relays in the user's language; no code or enum name reaches the data. Writes nothing.
/// </summary>
/// <param name="clientId">Optional. UUID of the employee; otherwise firstName/lastName</param>
/// <param name="firstName">Optional. First name of the employee</param>
/// <param name="lastName">Optional. Last name of the employee</param>
/// <param name="idNumber">Optional. Client number that tells apart people with the same name</param>
/// <param name="shiftId">Optional. UUID of the shift; otherwise shiftName</param>
/// <param name="shiftName">Optional. Name or abbreviation of the shift</param>
/// <param name="date">Required. The day of the booking</param>
/// <param name="analyseToken">Optional. Scenario token; omitted checks the real plan</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.ShiftSchedule;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("diagnose_work_booking")]
public class DiagnoseWorkBookingSkill : BaseSkillImplementation
{
    private const string ClientIdParameter = "clientId";
    private const string FirstNameParameter = "firstName";
    private const string LastNameParameter = "lastName";
    private const string ShiftIdParameter = "shiftId";
    private const string ShiftNameParameter = "shiftName";
    private const string DateParameter = "date";

    private const string MissingClientMessage = "Please provide the employee: clientId, or firstName and lastName.";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly IWorkBookingDiagnosisService _diagnosisService;

    public DiagnoseWorkBookingSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IShiftRepository shiftRepository,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IShiftGroupFilterService shiftGroupFilterService,
        IWorkBookingDiagnosisService diagnosisService)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _shiftRepository = shiftRepository;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftGroupFilterService = shiftGroupFilterService;
        _diagnosisService = diagnosisService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rawDate = GetParameter<string>(parameters, DateParameter);
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            return SkillResult.Error("Required parameter 'date' is missing. Ask the user for the day of the booking.");
        }

        if (!SkillCalendarStringParser.TryParseDateOnly(rawDate, context.UserLanguage, out var date))
        {
            return SkillResult.Error($"Invalid date: {rawDate}. Expected yyyy-MM-dd.");
        }

        var clientIdRaw = GetParameter<string>(parameters, ClientIdParameter);
        var firstName = GetParameter<string>(parameters, FirstNameParameter);
        var lastName = GetParameter<string>(parameters, LastNameParameter);
        if (string.IsNullOrWhiteSpace(clientIdRaw) && string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return SkillResult.Error(MissingClientMessage);
        }

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var (client, clientError) = await ClientResolver.ResolveByIdOrNameAsync(
            _clientSearchRepository, _clientRepository, _clientVisibilityGuard, clientIdRaw, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken,
            policy: NameMatchPolicy.Lenient);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(clientError ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var (shift, shiftError) = await ShiftResolver.ResolveByIdOrNameAsync(
            _shiftRepository, _shiftVisibilityGuard, _shiftGroupFilterService, GetParameter<string>(parameters, ShiftIdParameter),
            GetParameter<string>(parameters, ShiftNameParameter), date, ShiftIdParameter, cancellationToken);
        if (shift == null)
        {
            return SkillResult.Error(shiftError!);
        }

        var diagnosis = await _diagnosisService.DiagnoseAsync(
            new WorkBookingDiagnosisRequest(client.Id, shift.Id, date, analyseToken), cancellationToken);
        if (diagnosis.Outcome == WorkBookingDiagnosisOutcome.NotFound)
        {
            return SkillResult.Error(string.IsNullOrWhiteSpace(clientIdRaw)
                ? ClientResolver.NotFoundMessage(firstName, lastName)
                : $"Client {client.Id} not found.");
        }

        return BuildResult(diagnosis, date, analyseToken.HasValue, context.UserLanguage);
    }

    private static SkillResult BuildResult(WorkBookingDiagnosis diagnosis, DateOnly date, bool inScenario, string? language)
    {
        var day = date.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture);
        var reasonSentences = diagnosis.Refusals
            .Select(refusal => Fill(ReasonTemplate(refusal, name => name), refusal, diagnosis, day, name => name))
            .ToList();
        var messageSentences = diagnosis.Refusals
            .Select(refusal => Fill(
                ReasonTemplate(refusal, SkillMessageText.Name), refusal, diagnosis, day, SkillMessageText.Name))
            .ToList();
        var reasons = diagnosis.Refusals
            .Select((refusal, index) => new
            {
                Reason = reasonSentences[index],
                Remedy = WorkBookingDiagnosisTexts.Remedies[refusal.Remedy],
                RefusedBy = RefusedBy(refusal.Channels),
                Notes = Notes(refusal.Reason, inScenario),
                Conflicts = refusal.Conflicts
                    .Select(conflict => new
                    {
                        Comment = conflict.Code,
                        CommentParams = LocalizedCommentParams.ForLanguage(conflict.Params, language),
                    })
                    .ToList(),
            })
            .ToList();
        var warnings = diagnosis.Advisories
            .Select(advisory => new
            {
                advisory.Comment,
                Date = advisory.Date.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture),
                CommentParams = LocalizedCommentParams.ForLanguage(advisory.CommentParams, language),
            })
            .ToList();
        var verdict = WorkBookingDiagnosisTexts.Outcomes[diagnosis.Outcome];

        var data = new
        {
            Employee = diagnosis.ClientName,
            Shift = diagnosis.ShiftName,
            Date = day,
            Scenario = inScenario,
            Verdict = verdict,
            Reasons = reasons,
            WarningsIfBooked = warnings,
        };

        var reasonSummary = messageSentences.Count == 0 ? string.Empty : " " + string.Join(" ", messageSentences);
        var message =
            $"{SkillMessageText.Name(diagnosis.ClientName)}, {SkillMessageText.Name(diagnosis.ShiftName)}, {day}: " +
            $"{verdict}{reasonSummary} {WorkBookingDiagnosisTexts.RelayInstruction}";

        return SkillResult.SuccessResult(data, message);
    }

    private static string RefusedBy(WorkBookingChannels channels)
    {
        var sentences = new List<string>();
        if (channels.DirectEntry)
        {
            sentences.Add(WorkBookingDiagnosisTexts.DirectEntryRefuses);
        }

        if (channels.Assistant)
        {
            sentences.Add(WorkBookingDiagnosisTexts.AssistantRefuses);
        }

        if (channels.Grid)
        {
            sentences.Add(WorkBookingDiagnosisTexts.GridBlocks);
        }

        return string.Join(" ", sentences);
    }

    private static List<string> Notes(WorkBookingRefusalReason reason, bool inScenario)
    {
        var notes = new List<string>();
        if (reason is WorkBookingRefusalReason.SporadicShiftDayFull or WorkBookingRefusalReason.SporadicShiftRangeExhausted)
        {
            notes.Add(WorkBookingDiagnosisTexts.SporadicNotCheckedByAssistant);
        }

        if (inScenario && reason is WorkBookingRefusalReason.DayApproved or WorkBookingRefusalReason.PeriodClosed
                or WorkBookingRefusalReason.HardBlockingConflict)
        {
            notes.Add(WorkBookingDiagnosisTexts.ScenarioSkipsDirectCheck);
        }

        return notes;
    }

    private static string ReasonTemplate(WorkBookingRefusalFinding refusal, Func<string?, string?> name)
    {
        if (refusal.Reason != WorkBookingRefusalReason.CellOccupiedByWork)
        {
            return WorkBookingDiagnosisTexts.Reasons[refusal.Reason];
        }

        if (!refusal.Details.ContainsKey(WorkWriteConflictCodes.ShiftNameField))
        {
            return WorkBookingDiagnosisTexts.OccupiedByHiddenWork;
        }

        return string.IsNullOrWhiteSpace(name(Detail(refusal, WorkWriteConflictCodes.ShiftNameField)))
            ? WorkBookingDiagnosisTexts.OccupiedByUnnamedWork
            : WorkBookingDiagnosisTexts.Reasons[refusal.Reason];
    }

    private static string Fill(
        string template, WorkBookingRefusalFinding refusal, WorkBookingDiagnosis diagnosis, string day, Func<string?, string?> name)
    {
        var occupyingShift = refusal.Reason == WorkBookingRefusalReason.CellOccupiedByWork
            ? Detail(refusal, WorkWriteConflictCodes.ShiftNameField)
            : null;
        return template
            .Replace(WorkBookingDiagnosisTexts.EmployeeToken, name(diagnosis.ClientName) ?? string.Empty)
            .Replace(WorkBookingDiagnosisTexts.ShiftToken, name(diagnosis.ShiftName) ?? string.Empty)
            .Replace(WorkBookingDiagnosisTexts.OccupyingShiftToken, name(occupyingShift) ?? string.Empty)
            .Replace(WorkBookingDiagnosisTexts.DateToken, day)
            .Replace(WorkBookingDiagnosisTexts.FromToken, Detail(refusal, WorkBookingRefusalCodes.FromField) ?? SkillDisplayFormats.OpenEnd)
            .Replace(WorkBookingDiagnosisTexts.UntilToken, Detail(refusal, WorkBookingRefusalCodes.UntilField) ?? SkillDisplayFormats.OpenEnd)
            .Replace(WorkBookingDiagnosisTexts.CapacityToken, Detail(refusal, WorkWriteConflictCodes.CapacityField) ?? string.Empty)
            .Replace(WorkBookingDiagnosisTexts.RangeFromToken, Detail(refusal, WorkWriteConflictCodes.RangeFromField) ?? string.Empty)
            .Replace(WorkBookingDiagnosisTexts.RangeUntilToken, Detail(refusal, WorkWriteConflictCodes.RangeUntilField) ?? string.Empty);
    }

    private static string? Detail(WorkBookingRefusalFinding refusal, string field)
    {
        if (!refusal.Details.TryGetValue(field, out var value) || value == null)
        {
            return null;
        }

        return value switch
        {
            DateOnly dateValue => dateValue.ToString(SkillDisplayFormats.IsoDate, CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }
}
