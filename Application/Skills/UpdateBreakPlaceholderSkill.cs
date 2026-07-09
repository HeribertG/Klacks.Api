// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that moves, resizes, converts or annotates a planned absence (BreakPlaceholder) in the
/// absence calendar — the editable counterpart of a drag/resize/convert on the absence gantt.
/// Booked absences (Breaks) are read-only here and must be changed via update_break. The write is
/// self-verifying: it runs in a transaction and is re-read from the database before success is reported.
/// </summary>
/// <param name="placeholderId">Optional UUID of the placeholder; alternative to clientId + date</param>
/// <param name="clientId">UUID of the client, used with date to locate the placeholder</param>
/// <param name="date">A day (yyyy-MM-dd) covered by the existing placeholder</param>
/// <param name="newFromDate">Optional new first day of the absence</param>
/// <param name="newUntilDate">Optional new last day of the absence</param>
/// <param name="absenceId">Optional UUID of the new absence type (convert)</param>
/// <param name="information">Optional new free-text note</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateBreakPlaceholderSkill : BaseSkillImplementation
{
    private const string SkillName = "update_break_placeholder";

    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBreakPlaceholderSkill(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IAbsenceRepository absenceRepository,
        IUnitOfWork unitOfWork)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOptionalGuid(parameters, "placeholderId", out var placeholderId, out var idError))
        {
            return SkillResult.Error(idError!);
        }

        if (!TryGetOptionalGuid(parameters, "clientId", out var clientId, out var clientError))
        {
            return SkillResult.Error(clientError!);
        }

        var date = GetParameter<DateOnly?>(parameters, "date");
        var newFromDate = GetParameter<DateOnly?>(parameters, "newFromDate");
        var newUntilDate = GetParameter<DateOnly?>(parameters, "newUntilDate");
        var information = GetParameter<string>(parameters, "information");

        if (!TryGetOptionalGuid(parameters, "absenceId", out var newAbsenceId, out var absenceError))
        {
            return SkillResult.Error(absenceError!);
        }

        if (newFromDate is null && newUntilDate is null && newAbsenceId is null && information is null)
        {
            return SkillResult.Error(
                "Nothing to change: provide at least one of newFromDate, newUntilDate, absenceId or information.");
        }

        var (placeholder, resolveError) = await BreakPlaceholderResolver.ResolveAsync(
            _breakPlaceholderRepository, placeholderId, clientId, date, cancellationToken);
        if (placeholder is null)
        {
            return SkillResult.Error(resolveError!);
        }

        if (newAbsenceId.HasValue && !await _absenceRepository.Exists(newAbsenceId.Value))
        {
            return SkillResult.Error($"Absence type {newAbsenceId} not found. Use list_absence_types to resolve it.");
        }

        var targetFrom = newFromDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? placeholder.From;
        var targetUntil = newUntilDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? placeholder.Until;
        var targetAbsenceId = newAbsenceId ?? placeholder.AbsenceId;
        var targetInformation = information ?? placeholder.Information;

        if (targetUntil < targetFrom)
        {
            return SkillResult.Error(
                $"The resulting period is invalid: until ({targetUntil:yyyy-MM-dd}) would be before from ({targetFrom:yyyy-MM-dd}).");
        }

        var previous = new
        {
            FromDate = DateOnly.FromDateTime(placeholder.From),
            UntilDate = DateOnly.FromDateTime(placeholder.Until),
            placeholder.AbsenceId,
            placeholder.Information
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            placeholder.From = targetFrom;
            placeholder.Until = targetUntil;
            placeholder.AbsenceId = targetAbsenceId;
            placeholder.Information = targetInformation;

            await _unitOfWork.CompleteAsync();

            await ConfirmPersistedAsync(
                SkillName,
                () => _breakPlaceholderRepository.GetNoTracking(placeholder.Id),
                p => p.From == targetFrom && p.Until == targetUntil && p.AbsenceId == targetAbsenceId,
                $"planned absence {placeholder.Id}");

            return true;
        });

        return SkillResult.SuccessResult(
            new
            {
                PlaceholderId = placeholder.Id,
                placeholder.ClientId,
                Previous = previous,
                FromDate = DateOnly.FromDateTime(targetFrom),
                UntilDate = DateOnly.FromDateTime(targetUntil),
                AbsenceId = targetAbsenceId
            },
            $"Planned absence {placeholder.Id} updated to {targetFrom:yyyy-MM-dd}..{targetUntil:yyyy-MM-dd} " +
            "and confirmed in the database (verified). Relay the previous values so the change can be undone.");
    }

    private bool TryGetOptionalGuid(
        Dictionary<string, object> parameters,
        string name,
        out Guid? value,
        out string? error)
    {
        value = null;
        error = null;

        var raw = GetParameter<string>(parameters, name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!Guid.TryParse(raw, out var parsed))
        {
            error = $"Parameter '{name}' is not a valid UUID: '{raw}'.";
            return false;
        }

        value = parsed;
        return true;
    }
}
