// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Places the same absence (e.g. a training day / "Schulung") on every member of a named group for a
/// single date in one server-side round trip. With apply=false (default) it returns a read-only preview;
/// with apply=true it creates one Break per member that does not already have that absence on that date,
/// then re-reads the database (recount) to confirm how many were actually persisted. The break-creation
/// path runs through BulkAddBreaksCommand which has no transaction, so verification is by recount, not by
/// rollback — a partial result is reported honestly rather than silently claimed as success.
/// </summary>
/// <param name="groupName">Name (or partial name) of the group whose members get the absence.</param>
/// <param name="absenceType">Name of the absence type (e.g. "Schulung", "Ferien"); resolved to its id.</param>
/// <param name="date">The single date of the absence (ISO yyyy-MM-dd).</param>
/// <param name="apply">When true the absences are created; when false (default) only a preview is returned.</param>
/// <param name="workTime">Optional hours counted toward target hours; defaults to 8.</param>
/// <param name="information">Optional free-text note stored on every created absence.</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("bulk_add_absence_for_group")]
public class BulkAddAbsenceForGroupSkill : BaseSkillImplementation
{
    private const decimal DefaultWorkTime = 8m;

    private readonly IGroupRepository _groupRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _memberService;
    private readonly IBreakRepository _breakRepository;
    private readonly IMediator _mediator;

    public BulkAddAbsenceForGroupSkill(
        IGroupRepository groupRepository,
        IAbsenceRepository absenceRepository,
        IGetAllClientIdsFromGroupAndSubgroups memberService,
        IBreakRepository breakRepository,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _absenceRepository = absenceRepository;
        _memberService = memberService;
        _breakRepository = breakRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var absenceTypeName = GetRequiredString(parameters, "absenceType");
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;
        var workTime = GetParameter<decimal?>(parameters, "workTime") ?? DefaultWorkTime;
        var information = GetParameter<string>(parameters, "information");

        var groups = await _groupRepository.List();
        var (group, groupError) = GroupResolver.Resolve(groups, groupName);
        if (group == null)
        {
            return SkillResult.Error(groupError!);
        }

        var absences = await _absenceRepository.List();
        var typeMatches = absences
            .Where(a => !a.IsDeleted && NameMatches(a.Name, absenceTypeName))
            .ToList();
        if (typeMatches.Count == 0)
        {
            var availableTypes = absences.Where(a => !a.IsDeleted).Select(a => Label(a.Name)).ToList();
            var available = availableTypes.Count > 0
                ? "Available absence types: " + string.Join(", ", availableTypes) + "."
                : "There are no absence types yet.";
            return SkillResult.Error(
                $"Absence type '{absenceTypeName}' not found. {available} " +
                "Offer the user only these real absence types — do not invent types.");
        }

        if (typeMatches.Count > 1)
        {
            var names = string.Join(", ", typeMatches.Select(a => $"'{Label(a.Name)}'"));
            return SkillResult.Error(
                $"Multiple absence types match '{absenceTypeName}': {names}. Please be more specific.");
        }

        var absence = typeMatches[0];
        var typeLabel = Label(absence.Name);

        var members = await _memberService.GetAllClientIdsFromGroupAndSubgroups(group.Id);
        if (members.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { GroupName = group.Name, AbsenceType = typeLabel, Date = date, Attempted = 0, AddedCount = 0, VerifiedCount = 0, SkippedCount = 0 },
                $"Group '{group.Name}' has no members, so no absence was placed.");
        }

        var alreadyHave = (await _breakRepository
            .GetClientIdsWithBreakOnDate(members, date, absence.Id, cancellationToken)).ToHashSet();
        var toAdd = members.Where(id => !alreadyHave.Contains(id)).ToList();
        var skipped = members.Count - toAdd.Count;

        if (toAdd.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { GroupName = group.Name, AbsenceType = typeLabel, Date = date, Attempted = 0, AddedCount = 0, VerifiedCount = 0, SkippedCount = skipped },
                $"All {members.Count} member(s) of group '{group.Name}' already have '{typeLabel}' on {date}. Nothing to add.");
        }

        if (!apply)
        {
            var skippedNote = skipped > 0 ? $" ({skipped} already have it)" : string.Empty;
            return SkillResult.SuccessResult(
                new { GroupName = group.Name, AbsenceType = typeLabel, Date = date, Attempted = toAdd.Count, AddedCount = 0, VerifiedCount = 0, SkippedCount = skipped },
                $"Preview: '{typeLabel}' would be placed on {toAdd.Count} of {members.Count} member(s) of group " +
                $"'{group.Name}' on {date}{skippedNote}. Nothing was changed yet. " +
                "Ask the user to confirm, then call again with apply=true.");
        }

        var request = new BulkAddBreaksRequest
        {
            PeriodStart = date,
            PeriodEnd = date,
            Breaks = toAdd.Select(clientId => new BulkBreakItem
            {
                ClientId = clientId,
                AbsenceId = absence.Id,
                CurrentDate = date,
                StartTime = new TimeOnly(0, 0),
                EndTime = new TimeOnly(23, 59),
                WorkTime = workTime,
                Information = information,
                AnalyseToken = null
            }).ToList()
        };

        await _mediator.Send(new BulkAddBreaksCommand(request), cancellationToken);

        var confirmed = (await _breakRepository
            .GetClientIdsWithBreakOnDate(toAdd, date, absence.Id, cancellationToken)).Count;

        var skippedNote2 = skipped > 0 ? $" ({skipped} already had it)" : string.Empty;
        var verifyNote = confirmed < toAdd.Count
            ? $" WARNING: only {confirmed} of {toAdd.Count} could be confirmed in the database."
            : string.Empty;

        return SkillResult.SuccessResult(
            new { GroupName = group.Name, AbsenceType = typeLabel, Date = date, Attempted = toAdd.Count, AddedCount = toAdd.Count, VerifiedCount = confirmed, SkippedCount = skipped },
            $"Placed absence '{typeLabel}' on {toAdd.Count} member(s) of group '{group.Name}' on {date} " +
            $"and confirmed {confirmed} in the database (verified){skippedNote2}.{verifyNote}");
    }

    private static bool NameMatches(MultiLanguage name, string query) =>
        new[] { name.De, name.En, name.Fr, name.It }
            .Any(v => !string.IsNullOrWhiteSpace(v) &&
                      v!.Contains(query, StringComparison.OrdinalIgnoreCase));

    private static string Label(MultiLanguage name) =>
        new[] { name.De, name.En, name.Fr, name.It }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "absence";
}
