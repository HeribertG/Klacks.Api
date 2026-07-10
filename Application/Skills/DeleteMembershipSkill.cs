// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a membership (client affiliation period) via DeleteCommand&lt;MembershipResource&gt;.
/// Because Membership.ValidFrom is the plannability boundary in the schedule, removing a membership
/// removes the client's affiliation period. Use list_client_memberships to resolve the id first.
/// The delete is self-verifying: the membership is re-read afterwards and success is only reported
/// when it is no longer visible.
/// </summary>
/// <param name="membershipId">Required. UUID of the membership to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_membership")]
public class DeleteMembershipSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteMembershipSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var membershipId = GetRequiredGuid(parameters, "membershipId");

        var deleted = await _mediator.Send(new DeleteCommand<MembershipResource>(membershipId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Membership {membershipId} not found.");
        }

        MembershipResource? stillVisible;
        try
        {
            stillVisible = await _mediator.Send(new GetQuery<MembershipResource>(membershipId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            stillVisible = null;
        }

        if (stillVisible != null)
        {
            return SkillResult.Error(
                $"Database verification failed: membership '{membershipId}' is still visible after the delete. " +
                "Use list_client_memberships to check the current state before retrying.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id, deleted.ClientId },
            "Membership deleted and confirmed removed from the database (verified). " +
            "Note: ValidFrom was the plannability boundary in the schedule.");
    }
}
