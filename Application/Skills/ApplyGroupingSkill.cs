// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the grouping for the given entity type: persists the moves the dry run showed, assigning
/// every client of that type to the group whose name matches its address city or — when no group name
/// matches — to the nearest group that carries coordinates, and ending the coarser memberships the move
/// replaces. This is the explicit "do it now" step — call it only after the user has confirmed the
/// proposal from propose_grouping. Recomputes the proposal internally so it always matches a fresh dry
/// run, and reports how many memberships actually ended so the number the proposal announced can be
/// confirmed.
/// </summary>
/// <param name="entityType">Which client population to apply the grouping to: Employee, ExternEmp, or Customer. Must match the entityType used in the preceding propose_grouping call.</param>

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("apply_grouping")]
public class ApplyGroupingSkill : BaseSkillImplementation
{
    private const string EntityTypeParameterName = "entityType";

    private readonly IMediator _mediator;

    public ApplyGroupingSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entityTypeValue = GetRequiredString(parameters, EntityTypeParameterName);
        if (!Enum.TryParse<EntityTypeEnum>(entityTypeValue, ignoreCase: true, out var entityType))
        {
            return SkillResult.Error(
                $"Invalid {EntityTypeParameterName} '{entityTypeValue}'. Must be one of: " +
                $"{EntityTypeEnum.Employee}, {EntityTypeEnum.ExternEmp}, {EntityTypeEnum.Customer}.");
        }

        var noun = GroupingEntityNouns.Noun(entityType);

        CustomerGroupingApplyResult result;
        try
        {
            result = await _mediator.Send(new ApplyCustomerGroupingCommand(entityType), cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            result,
            $"Applied: {result.MovedCount} {noun}(s) moved to their location group " +
            $"(confirmed {result.VerifiedCount} new memberships in the database, verified); " +
            $"{result.EndedMembershipCount} existing group membership(s) ended as part of these moves; " +
            $"{result.UnassignedCount} could not be assigned.");
    }
}
