// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing membership (client affiliation period). Only fields supplied as parameters
/// are changed. The membership's ValidFrom is the plannability boundary in the schedule, so moving
/// it changes from which date the client can be planned. The write is self-verifying: the membership
/// is re-read after the update and success is only reported when the persisted values match.
/// </summary>
/// <param name="membershipId">Required. UUID of the membership to update.</param>
/// <param name="validFrom">Optional. New start date (YYYY-MM-DD); plannability boundary in the schedule.</param>
/// <param name="validUntil">Optional. New end date (YYYY-MM-DD).</param>
/// <param name="clearValidUntil">Optional. If true, removes the end date (open-ended membership).</param>
/// <param name="type">Optional. New numeric membership type code.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Common;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_membership")]
public class UpdateMembershipSkill : BaseSkillImplementation
{
    private const string SkillName = "update_membership";
    private const string ValidFromField = "validFrom";

    private readonly IMediator _mediator;
    private readonly IPendingConfirmationStore _confirmationStore;

    public UpdateMembershipSkill(IMediator mediator, IPendingConfirmationStore confirmationStore)
    {
        _mediator = mediator;
        _confirmationStore = confirmationStore;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var membershipId = GetRequiredGuid(parameters, "membershipId");

        MembershipResource membership;
        try
        {
            membership = await _mediator.Send(new GetQuery<MembershipResource>(membershipId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Membership '{membershipId}' not found.");
        }

        var changed = new List<string>();

        var validFrom = GetParameter<DateTime?>(parameters, "validFrom");
        if (validFrom.HasValue && validFrom.Value != membership.ValidFrom)
        {
            membership.ValidFrom = validFrom.Value;
            changed.Add(ValidFromField);
        }

        var clearValidUntil = GetParameter<bool>(parameters, "clearValidUntil", false);
        if (clearValidUntil)
        {
            if (membership.ValidUntil != null)
            {
                membership.ValidUntil = null;
                changed.Add("validUntil");
            }
        }
        else
        {
            var validUntil = GetParameter<DateTime?>(parameters, "validUntil");
            if (validUntil.HasValue && validUntil.Value != membership.ValidUntil)
            {
                membership.ValidUntil = validUntil.Value;
                changed.Add("validUntil");
            }
        }

        var type = GetParameter<int?>(parameters, "type");
        if (type.HasValue && type.Value != membership.Type)
        {
            membership.Type = type.Value;
            changed.Add("type");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { MembershipId = membershipId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — membership left unchanged.");
        }

        if (membership.ValidUntil.HasValue && membership.ValidUntil.Value < membership.ValidFrom)
        {
            return SkillResult.Error("validUntil must not be before validFrom.");
        }

        var validFromConfirmed = GetParameter<bool>(
            parameters, MembershipPlausibilityDefaults.ValidFromConfirmedParameter, false);
        if (!validFromConfirmed && changed.Contains(ValidFromField))
        {
            DateTime? birthdate = null;
            try
            {
                var client = await _mediator.Send(
                    new GetQuery<ClientResource>(membership.ClientId), cancellationToken);
                birthdate = client.Birthdate;
            }
            catch (KeyNotFoundException)
            {
            }

            var reason = MembershipValidFromPlausibility.Evaluate(membership.ValidFrom, birthdate, DateTime.Today);
            if (reason != null)
            {
                return ValidFromConfirmationFactory.RequireConfirmation(
                    _confirmationStore, context.UserId, SkillName, parameters, reason);
            }
        }

        var updated = await _mediator.Send(new PutCommand<MembershipResource>(membership), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating membership '{membershipId}' failed.");
        }

        MembershipResource persisted;
        try
        {
            persisted = await _mediator.Send(new GetQuery<MembershipResource>(membershipId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error(
                $"Database verification failed: membership '{membershipId}' could not be read back after the update. " +
                "Use list_client_memberships to check the current state before retrying.");
        }

        if (persisted.ValidFrom != membership.ValidFrom
            || persisted.ValidUntil != membership.ValidUntil
            || persisted.Type != membership.Type)
        {
            return SkillResult.Error(
                $"Database verification failed: membership '{membershipId}' does not show the requested values after the update. " +
                "Use list_client_memberships to check the current state before retrying.");
        }

        return SkillResult.SuccessResult(
            new
            {
                MembershipId = membershipId,
                ChangedFields = changed,
                persisted.ClientId,
                persisted.Type,
                persisted.ValidFrom,
                persisted.ValidUntil
            },
            $"Membership updated ({string.Join(", ", changed)}) and confirmed in the database (verified). " +
            "ValidFrom is the plannability boundary in the schedule.");
    }
}
