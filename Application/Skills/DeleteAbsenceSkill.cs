// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an absence type. Refuses if the type is in use by active breaks or
/// break-placeholders, or if the type is marked as Undeletable. Use list_absence_types
/// to see which types are safe to remove.
/// </summary>
/// <param name="absenceId">Required. UUID of the absence type to delete.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_absence")]
public class DeleteAbsenceSkill : BaseSkillImplementation
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAbsenceSkill(IAbsenceRepository absenceRepository, IUnitOfWork unitOfWork)
    {
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var absenceId = GetRequiredGuid(parameters, "absenceId");

        var absence = await _absenceRepository.Get(absenceId);
        if (absence == null)
        {
            return SkillResult.Error($"Absence type '{absenceId}' not found.");
        }

        if (absence.Undeletable)
        {
            return SkillResult.Error($"Absence type '{absence.Name.De}' is marked as Undeletable and cannot be removed.");
        }

        var activeBreaks = await _absenceRepository.CountActiveBreaksByAbsenceAsync(absenceId, cancellationToken);
        var activePlaceholders = await _absenceRepository.CountActiveBreakPlaceholdersByAbsenceAsync(absenceId, cancellationToken);
        if (activeBreaks > 0 || activePlaceholders > 0)
        {
            return SkillResult.Error(
                $"Absence type '{absence.Name.De}' is still in use ({activeBreaks} break(s), {activePlaceholders} placeholder(s)). Reassign or remove them first.");
        }

        var nameDe = absence.Name.De ?? absenceId.ToString();

        await _absenceRepository.Delete(absenceId);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { AbsenceId = absenceId, DeletedNameDe = nameDe },
            $"Absence type '{nameDe}' was soft-deleted.");
    }
}
