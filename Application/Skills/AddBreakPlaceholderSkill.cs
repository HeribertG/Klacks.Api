// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that records a vacation/absence WISH as a BreakPlaceholder over a date range. A placeholder
/// is a pre-booking in the absence calendar — it does not place Breaks in the schedule and stays the
/// weakest planning layer until a planner materialises it (add_break places the real absence).
/// </summary>
/// <param name="clientId">UUID of the client (employee or extern).</param>
/// <param name="absenceId">UUID of the absence type (resolve via list_absence_types).</param>
/// <param name="fromDate">First day of the wished period in ISO yyyy-MM-dd.</param>
/// <param name="untilDate">Last day of the wished period in ISO yyyy-MM-dd (equal to fromDate for one day).</param>
/// <param name="information">Optional free-text note, e.g. the reason quoted from the request.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_break_placeholder")]
public class AddBreakPlaceholderSkill : BaseSkillImplementation
{
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddBreakPlaceholderSkill(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IAbsenceRepository absenceRepository,
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _absenceRepository = absenceRepository;
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var absenceId = GetRequiredGuid(parameters, "absenceId");
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate")
            ?? throw new ArgumentException("Required parameter 'fromDate' is missing");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate")
            ?? throw new ArgumentException("Required parameter 'untilDate' is missing");
        var information = GetParameter<string>(parameters, "information");

        if (untilDate < fromDate)
        {
            return SkillResult.Error($"untilDate ({untilDate}) must not be before fromDate ({fromDate}).");
        }

        if (!await _clientRepository.Exists(clientId))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        if (!await _absenceRepository.Exists(absenceId))
        {
            return SkillResult.Error($"Absence type {absenceId} not found. Use list_absence_types to resolve it.");
        }

        var entity = new BreakPlaceholder
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            AbsenceId = absenceId,
            From = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Until = untilDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Information = information,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        await _breakPlaceholderRepository.Add(entity);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                Id = entity.Id,
                ClientId = clientId,
                AbsenceId = absenceId,
                FromDate = fromDate,
                UntilDate = untilDate
            },
            $"Absence wish recorded as placeholder for client {clientId} from {fromDate} to {untilDate}. " +
            "It appears in the absence calendar as a pre-booking; use add_break to materialise it in the schedule.");
    }
}
