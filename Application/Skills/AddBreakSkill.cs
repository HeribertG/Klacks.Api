// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that places a single absence (Break) on a client/date — vacation, sick day, training day etc.
/// Wraps BulkAddBreaksCommand so the Break-macro-service runs and period-hour recalculation kicks in.
/// </summary>
/// <param name="clientId">UUID of the client.</param>
/// <param name="absenceId">UUID of the Absence type (vacation/sick/etc.).</param>
/// <param name="date">Workday in ISO yyyy-MM-dd.</param>
/// <param name="startTime">Optional HH:mm; defaults to 00:00.</param>
/// <param name="endTime">Optional HH:mm; defaults to 23:59.</param>
/// <param name="workTime">Optional hours; defaults to 8h (typical full-day absence). Counts toward TargetHours but NOT toward MaxWeeklyHours.</param>
/// <param name="information">Free-text note.</param>
/// <param name="analyseToken">Optional scenario UUID; null = main schedule.</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_break")]
public class AddBreakSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IClientRepository _clientRepository;

    public AddBreakSkill(
        IMediator mediator,
        IAbsenceRepository absenceRepository,
        IClientRepository clientRepository)
    {
        _mediator = mediator;
        _absenceRepository = absenceRepository;
        _clientRepository = clientRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var absenceId = GetRequiredGuid(parameters, "absenceId");
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var startTimeRaw = GetParameter<string>(parameters, "startTime");
        var endTimeRaw = GetParameter<string>(parameters, "endTime");
        var workTimeRaw = GetParameter<decimal?>(parameters, "workTime");
        var information = GetParameter<string>(parameters, "information");
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");

        if (!await _clientRepository.Exists(clientId))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        if (!await _absenceRepository.Exists(absenceId))
        {
            return SkillResult.Error($"Absence type {absenceId} not found. Use list_absence_types to see available IDs.");
        }

        var startTime = !string.IsNullOrWhiteSpace(startTimeRaw) && TimeOnly.TryParse(startTimeRaw, out var sParsed)
            ? sParsed : new TimeOnly(0, 0);
        var endTime = !string.IsNullOrWhiteSpace(endTimeRaw) && TimeOnly.TryParse(endTimeRaw, out var eParsed)
            ? eParsed : new TimeOnly(23, 59);
        var workTime = workTimeRaw ?? 8m;

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw) && Guid.TryParse(analyseTokenRaw, out var atParsed))
        {
            analyseToken = atParsed;
        }

        var request = new BulkAddBreaksRequest
        {
            PeriodStart = date,
            PeriodEnd = date,
            Breaks =
            [
                new BulkBreakItem
                {
                    ClientId = clientId,
                    AbsenceId = absenceId,
                    CurrentDate = date,
                    StartTime = startTime,
                    EndTime = endTime,
                    WorkTime = workTime,
                    Information = information,
                    AnalyseToken = analyseToken
                }
            ]
        };

        var response = await _mediator.Send(new BulkAddBreaksCommand(request), cancellationToken);
        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                AbsenceId = absenceId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                WorkTime = workTime,
                AnalyseToken = analyseToken,
                BulkResponse = response
            },
            $"Break placed for client {clientId} on {date} (absence {absenceId}, {workTime}h). " +
            "Counts toward TargetHours; immutable for wizards.");
    }
}
