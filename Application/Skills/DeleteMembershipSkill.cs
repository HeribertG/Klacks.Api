// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a membership (client affiliation period) via DeleteCommand&lt;MembershipResource&gt;.
/// Because Membership.ValidFrom is the plannability boundary in the schedule, removing a membership
/// removes the client's affiliation period. Use list_client_memberships to resolve the id first.
/// </summary>
/// <param name="membershipId">Required. UUID of the membership to delete.</param>

using Klacks.Api.Application.Commands;
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

        return SkillResult.SuccessResult(
            new { deleted.Id, deleted.ClientId },
            "Membership deleted. Note: ValidFrom was the plannability boundary in the schedule.");
    }
}
