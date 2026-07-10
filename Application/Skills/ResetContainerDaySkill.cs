// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resets a single day of a container shift back to its template ("Dienst zurücksetzen") by deleting
/// the day-specific <see cref="Klacks.Api.Application.Commands.ContainerShiftOverrides.DeleteContainerShiftOverrideCommand"/>
/// override, if one exists. Deleting an override does not use the optimistic container lock — the
/// command handler instead refuses the delete with a <see cref="ConflictException"/> when work entries
/// already exist for that container and date. A missing override is not an error: the day already
/// follows the template, so there is nothing to reset.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift.</param>
/// <param name="date">Required. ISO date of the day to reset, e.g. 2026-07-09.</param>

using Klacks.Api.Application.Commands.ContainerShiftOverrides;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Queries.ContainerShiftOverrides;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reset_container_day")]
public class ResetContainerDaySkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;

    public ResetContainerDaySkill(IShiftRepository shiftRepository, IMediator mediator)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerIdRaw = GetParameter<string>(parameters, "containerId");
        if (string.IsNullOrWhiteSpace(containerIdRaw) || !Guid.TryParse(containerIdRaw, out var containerId))
        {
            return SkillResult.Error("Please provide a valid container id (UUID).");
        }

        var dateRaw = GetParameter<string>(parameters, "date");
        if (string.IsNullOrWhiteSpace(dateRaw) || !DateOnly.TryParse(dateRaw, out var date))
        {
            return SkillResult.Error("Please give a date in ISO format, e.g. 2026-07-09.");
        }

        var shift = await _shiftRepository.Get(containerId);
        if (shift is null || shift.ShiftType != ShiftType.IsContainer)
        {
            return SkillResult.Error($"Shift {containerId} is not a container shift.");
        }

        var existing = await _mediator.Send(new GetContainerShiftOverrideQuery(containerId, date), cancellationToken);
        if (existing is null)
        {
            return SkillResult.SuccessResult(
                new { ContainerId = containerId, Date = date, Reset = false },
                $"No override exists for {date:yyyy-MM-dd} — this day already follows the template, nothing to reset.");
        }

        try
        {
            var deleted = await _mediator.Send(new DeleteContainerShiftOverrideCommand(containerId, existing.Id), cancellationToken);
            if (!deleted)
            {
                return SkillResult.Error($"Override for {date:yyyy-MM-dd} could not be found when attempting to delete it (it may have just been reset by someone else).");
            }
        }
        catch (ConflictException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var verification = await _mediator.Send(new GetContainerShiftOverrideQuery(containerId, date), cancellationToken);
        if (verification is not null)
        {
            return SkillResult.Error("Database verification failed: the override still exists after the delete — please retry.");
        }

        return SkillResult.SuccessResult(
            new { ContainerId = containerId, Date = date, Reset = true, Verified = true },
            $"Override for {date:yyyy-MM-dd} removed — the day now falls back to the template (verified).");
    }
}
