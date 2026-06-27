// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the geographic employee grouping: persists the moves that propose_employee_grouping showed,
/// assigning every employee to its nearest location group and retiring the coarser memberships it
/// replaces. This is the explicit "do it now" step — call it only after the user has confirmed the
/// proposal. Recomputes the proposal internally so it always matches a fresh dry run.
/// </summary>

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("apply_employee_grouping")]
public class ApplyEmployeeGroupingSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ApplyEmployeeGroupingSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        CustomerGroupingApplyResult result;
        try
        {
            result = await _mediator.Send(
                new ApplyCustomerGroupingCommand(EntityTypeEnum.Employee), cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            result,
            $"Applied: {result.MovedCount} employee(s) moved to their nearest location group " +
            $"(confirmed {result.VerifiedCount} new memberships in the database, verified); " +
            $"{result.UnassignedCount} could not be assigned.");
    }
}
