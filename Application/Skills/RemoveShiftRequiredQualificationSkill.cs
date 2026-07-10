// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes a qualification requirement previously set on a shift via set_shift_required_qualification.
/// Thin wrapper around <see cref="Klacks.Api.Application.Commands.Qualifications.DeleteShiftRequiredQualificationCommand"/>.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift.</param>
/// <param name="qualificationId">Required. UUID of the qualification to remove from the shift.</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("remove_shift_required_qualification")]
public class RemoveShiftRequiredQualificationSkill : BaseSkillImplementation
{
    private readonly IShiftRequiredQualificationRepository _repository;
    private readonly IMediator _mediator;

    public RemoveShiftRequiredQualificationSkill(
        IShiftRequiredQualificationRepository repository,
        IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var qualificationId = GetRequiredGuid(parameters, "qualificationId");

        var existing = await _repository.GetActiveAsync(shiftId, qualificationId, cancellationToken);
        if (existing == null)
        {
            var current = await _repository.GetByShiftIdAsync(shiftId, cancellationToken);
            if (current.Count == 0)
            {
                return SkillResult.Error(
                    $"Shift {shiftId} has no required qualification with id {qualificationId} — " +
                    "in fact it has no required qualifications at all.");
            }

            var currentIds = string.Join(", ", current.Select(q => q.QualificationId));
            return SkillResult.Error(
                $"Shift {shiftId} has no required qualification with id {qualificationId}. " +
                $"Currently required qualification ids: {currentIds}.");
        }

        await _mediator.Send(new DeleteShiftRequiredQualificationCommand(existing.Id), cancellationToken);

        return SkillResult.SuccessResult(
            new { ShiftId = shiftId, QualificationId = qualificationId, existing.Id },
            $"Shift {shiftId} no longer requires the qualification {qualificationId}.");
    }
}
