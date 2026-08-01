// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes an individual period scheme via DeleteCommand.
/// </summary>
/// <param name="individualPeriodId">UUID of the scheme to delete (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_individual_period")]
public class DeleteIndividualPeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteIndividualPeriodSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var individualPeriodId = GetRequiredGuid(parameters, "individualPeriodId");

        IndividualPeriodResource? deleted;
        try
        {
            deleted = await _mediator.Send(
                new DeleteCommand<IndividualPeriodResource>(individualPeriodId), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (deleted == null)
        {
            return SkillResult.Error($"Individual period scheme {individualPeriodId} not found.");
        }

        return SkillResult.SuccessResult(
            new { IndividualPeriodId = individualPeriodId, deleted.Name },
            $"Individual period scheme '{deleted.Name}' removed.");
    }
}
