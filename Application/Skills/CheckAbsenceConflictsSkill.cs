// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only advisory skill that checks whether a planned absence period is feasible for a client
/// BEFORE it is entered: overlapping planned absences (placeholders), overlapping booked absences
/// (Breaks), scheduled work assignments in the period, and the client's membership window. The
/// server itself enforces none of these — this skill is the assistant's pre-flight check.
/// </summary>
/// <param name="clientId">UUID of the client the absence is planned for</param>
/// <param name="fromDate">First day of the planned period (yyyy-MM-dd)</param>
/// <param name="untilDate">Last day of the planned period (yyyy-MM-dd)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("check_absence_conflicts")]
public class CheckAbsenceConflictsSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IAbsenceRepository _absenceRepository;

    public CheckAbsenceConflictsSkill(
        IClientRepository clientRepository,
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IBreakRepository breakRepository,
        IWorkRepository workRepository,
        IAbsenceRepository absenceRepository)
    {
        _clientRepository = clientRepository;
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _breakRepository = breakRepository;
        _workRepository = workRepository;
        _absenceRepository = absenceRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate")
            ?? throw new ArgumentException("Required parameter 'fromDate' is missing");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate")
            ?? throw new ArgumentException("Required parameter 'untilDate' is missing");

        if (untilDate < fromDate)
        {
            return SkillResult.Error($"untilDate ({untilDate}) must not be before fromDate ({fromDate}).");
        }

        var client = await _clientRepository.Get(clientId);
        if (client is null)
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilUtc = untilDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var absences = await _absenceRepository.List();
        var typeLabels = absences.ToDictionary(a => a.Id, a => Label(a.Name));

        var plannedOverlaps = (await _breakPlaceholderRepository
            .GetByClientAndRangeAsync(clientId, fromUtc, untilUtc, cancellationToken))
            .Select(bp => new
            {
                PlaceholderId = bp.Id,
                AbsenceType = typeLabels.GetValueOrDefault(bp.AbsenceId, "Unknown"),
                From = DateOnly.FromDateTime(bp.From).ToString("yyyy-MM-dd"),
                Until = DateOnly.FromDateTime(bp.Until).ToString("yyyy-MM-dd")
            })
            .ToList();

        var bookedOverlaps = (await _breakRepository
            .GetByClientAndDateRangeAsync(clientId, fromDate, untilDate, cancellationToken))
            .Select(b => new
            {
                BreakId = b.Id,
                AbsenceType = typeLabels.GetValueOrDefault(b.AbsenceId, "Unknown"),
                Date = b.CurrentDate.ToString("yyyy-MM-dd")
            })
            .ToList();

        var scheduledWork = (await _workRepository
            .GetByClientAndDateRangeAsync(clientId, fromUtc, untilUtc, cancellationToken))
            .Where(w => w.AnalyseToken == null)
            .Select(w => new
            {
                WorkId = w.Id,
                Date = w.CurrentDate.ToString("yyyy-MM-dd"),
                w.LockLevel
            })
            .ToList();

        string? membershipWarning = null;
        if (client.Membership is null)
        {
            membershipWarning = "The client has no membership record; plannability cannot be confirmed.";
        }
        else
        {
            var memberFrom = DateOnly.FromDateTime(client.Membership.ValidFrom);
            var memberUntil = client.Membership.ValidUntil.HasValue
                ? DateOnly.FromDateTime(client.Membership.ValidUntil.Value)
                : (DateOnly?)null;

            if (fromDate < memberFrom)
            {
                membershipWarning = $"The period starts before the client's membership begins ({memberFrom:yyyy-MM-dd}).";
            }
            else if (memberUntil.HasValue && untilDate > memberUntil.Value)
            {
                membershipWarning = $"The period ends after the client's membership ends ({memberUntil:yyyy-MM-dd}).";
            }
        }

        var feasible = plannedOverlaps.Count == 0 && bookedOverlaps.Count == 0 && membershipWarning is null;

        var clientName = $"{client.FirstName} {client.Name}".Trim();
        var issues = new List<string>();
        if (plannedOverlaps.Count > 0)
        {
            issues.Add($"{plannedOverlaps.Count} overlapping planned absence(s)");
        }

        if (bookedOverlaps.Count > 0)
        {
            issues.Add($"{bookedOverlaps.Count} overlapping booked absence day(s)");
        }

        if (scheduledWork.Count > 0)
        {
            issues.Add($"{scheduledWork.Count} scheduled work assignment(s) that would need replacement");
        }

        if (membershipWarning is not null)
        {
            issues.Add("a membership period problem");
        }

        var message = feasible
            ? $"The period {fromDate:yyyy-MM-dd}..{untilDate:yyyy-MM-dd} is free of absence conflicts for {clientName}." +
              (scheduledWork.Count > 0
                  ? $" Note: {scheduledWork.Count} scheduled work assignment(s) fall into the period and would need to be re-planned (e.g. via cover_absence)."
                  : string.Empty)
            : $"The period {fromDate:yyyy-MM-dd}..{untilDate:yyyy-MM-dd} has issues for {clientName}: " +
              string.Join(", ", issues) + ". Relay the details to the user before entering the absence.";

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                ClientName = clientName,
                FromDate = fromDate,
                UntilDate = untilDate,
                Feasible = feasible,
                PlannedAbsenceOverlaps = plannedOverlaps,
                BookedAbsenceOverlaps = bookedOverlaps,
                ScheduledWorkInPeriod = scheduledWork,
                MembershipWarning = membershipWarning
            },
            message);
    }

    private static string Label(MultiLanguage name) =>
        new[] { name.De, name.En, name.Fr, name.It }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "absence";
}
