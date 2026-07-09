// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that removes a planned absence (BreakPlaceholder) from the absence calendar — the inverse
/// of add_break_placeholder. Booked absences (Breaks) are read-only in the absence calendar; when
/// only a booked absence exists on the given day the skill reports its breakId and points to
/// delete_break instead. The soft-delete is self-verifying: it runs in a transaction and the row
/// must be gone from the filtered view before success is reported.
/// </summary>
/// <param name="placeholderId">Optional UUID of the placeholder; alternative to clientId + date</param>
/// <param name="clientId">UUID of the client, used with date to locate the placeholder</param>
/// <param name="date">A day (yyyy-MM-dd) covered by the planned absence to remove</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class DeleteBreakPlaceholderSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_break_placeholder";

    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBreakPlaceholderSkill(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IBreakRepository breakRepository,
        IUnitOfWork unitOfWork)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _breakRepository = breakRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var placeholderIdRaw = GetParameter<string>(parameters, "placeholderId");
        Guid? placeholderId = null;
        if (!string.IsNullOrWhiteSpace(placeholderIdRaw))
        {
            if (!Guid.TryParse(placeholderIdRaw, out var parsedId))
            {
                return SkillResult.Error($"Parameter 'placeholderId' is not a valid UUID: '{placeholderIdRaw}'.");
            }

            placeholderId = parsedId;
        }

        var clientIdRaw = GetParameter<string>(parameters, "clientId");
        Guid? clientId = null;
        if (!string.IsNullOrWhiteSpace(clientIdRaw))
        {
            if (!Guid.TryParse(clientIdRaw, out var parsedClientId))
            {
                return SkillResult.Error($"Parameter 'clientId' is not a valid UUID: '{clientIdRaw}'.");
            }

            clientId = parsedClientId;
        }

        var date = GetParameter<DateOnly?>(parameters, "date");

        var (placeholder, resolveError) = await BreakPlaceholderResolver.ResolveAsync(
            _breakPlaceholderRepository, placeholderId, clientId, date, cancellationToken);

        if (placeholder is null)
        {
            if (clientId.HasValue && date.HasValue)
            {
                var booked = await _breakRepository.GetByClientAndDateRangeAsync(
                    clientId.Value, date.Value, date.Value, cancellationToken);
                if (booked.Count > 0)
                {
                    return SkillResult.Error(
                        $"{resolveError} However, a BOOKED absence exists on {date:yyyy-MM-dd} " +
                        $"(breakId {booked[0].Id}). Booked absences are read-only in the absence calendar — " +
                        "use delete_break with this breakId to remove it.");
                }
            }

            return SkillResult.Error(resolveError!);
        }

        var removed = new
        {
            PlaceholderId = placeholder.Id,
            placeholder.ClientId,
            placeholder.AbsenceId,
            FromDate = DateOnly.FromDateTime(placeholder.From),
            UntilDate = DateOnly.FromDateTime(placeholder.Until)
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _breakPlaceholderRepository.Delete(placeholder.Id);
            await _unitOfWork.CompleteAsync();

            await ConfirmDeletedAsync<Domain.Models.Schedules.BreakPlaceholder>(
                SkillName,
                () => _breakPlaceholderRepository.GetNoTracking(placeholder.Id),
                $"planned absence {placeholder.Id}");

            return true;
        });

        return SkillResult.SuccessResult(
            removed,
            $"Planned absence {removed.PlaceholderId} ({removed.FromDate}..{removed.UntilDate}) removed " +
            "and confirmed gone in the database (verified). It can be restored via add_break_placeholder " +
            "with the same values.");
    }
}
