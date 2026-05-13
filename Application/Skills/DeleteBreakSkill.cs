// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that soft-deletes a single Break (absence) entry via DeleteBreakCommand. Period range is
/// taken from the break itself so period-hour recalculation kicks in correctly. Inverse of add_break.
/// </summary>
/// <param name="breakId">UUID of the Break entry to delete.</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_break")]
public class DeleteBreakSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IBreakRepository _breakRepository;

    public DeleteBreakSkill(IMediator mediator, IBreakRepository breakRepository)
    {
        _mediator = mediator;
        _breakRepository = breakRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var breakId = GetRequiredGuid(parameters, "breakId");

        var existing = await _breakRepository.Get(breakId);
        if (existing == null)
        {
            return SkillResult.Error($"Break {breakId} not found.");
        }

        var date = existing.CurrentDate;
        var result = await _mediator.Send(new DeleteBreakCommand(breakId, date, date), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error($"Delete of break {breakId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                BreakId = breakId,
                ClientId = existing.ClientId,
                AbsenceId = existing.AbsenceId,
                Date = date
            },
            $"Break {breakId} (client {existing.ClientId} on {date}) deleted.");
    }
}
