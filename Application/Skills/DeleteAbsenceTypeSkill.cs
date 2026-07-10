// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an absence type (master data). Refuses undeletable seeded types and types
/// that are still used by active absence bookings or placeholders — the counts are reported
/// so the user knows what blocks the delete. Verified by re-reading the type.
/// </summary>
/// <param name="absenceTypeId">Optional. UUID of the absence type; takes precedence over typeName.</param>
/// <param name="typeName">Optional. Name of the type in any core language.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class DeleteAbsenceTypeSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_absence_type";

    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAbsenceTypeSkill(IAbsenceRepository absenceRepository, IUnitOfWork unitOfWork)
    {
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (absence, resolveError) = await AbsenceTypeResolver.ResolveAsync(
            GetParameter<string>(parameters, "absenceTypeId"),
            GetParameter<string>(parameters, "typeName"),
            _absenceRepository);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        if (absence!.Undeletable)
        {
            return SkillResult.Error(
                $"Absence type '{absence.Name.De}' is a protected seeded type and cannot be deleted. " +
                "Use update_absence_type (e.g. hideInGantt=true) to retire it from view instead.");
        }

        var activeBreaks = await _absenceRepository.CountActiveBreaksByAbsenceAsync(absence.Id, cancellationToken);
        var activePlaceholders = await _absenceRepository.CountActiveBreakPlaceholdersByAbsenceAsync(
            absence.Id, cancellationToken);
        if (activeBreaks > 0 || activePlaceholders > 0)
        {
            return SkillResult.Error(
                $"Absence type '{absence.Name.De}' is still in use: {activeBreaks} active booking(s) and " +
                $"{activePlaceholders} placeholder(s). Remove or rebook them first — the type is not deleted.");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _absenceRepository.Delete(absence.Id);
                await _unitOfWork.CompleteAsync();
                await ConfirmDeletedAsync(
                    SkillName,
                    () => _absenceRepository.GetNoTracking(absence.Id),
                    $"absence type '{absence.Name.De}'");
                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new { AbsenceTypeId = absence.Id, Name = absence.Name.De },
            $"Absence type '{absence.Name.De}' was deleted and confirmed in the database (verified).");
    }
}
