// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Common;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
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
    private readonly ICompanyClock _companyClock;

    public UpdateMembershipSkill(
        IMediator mediator, IPendingConfirmationStore confirmationStore, ICompanyClock companyClock)
    {
        _mediator = mediator;
        _confirmationStore = confirmationStore;
        _companyClock = companyClock;
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

        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var dateError = ApplyValidFrom(membership, parameters, today, context.UserLanguage, changed)
            ?? ApplyValidUntil(membership, parameters, today, context.UserLanguage, changed);
        if (dateError != null)
        {
            return dateError;
        }

        var changeSetResult = CheckChangeSet(membershipId, membership, changed);
        if (changeSetResult != null)
        {
            return changeSetResult;
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

            var reason = MembershipValidFromPlausibility.Evaluate(membership.ValidFrom, birthdate, today);
            if (reason != null)
            {
                return ValidFromConfirmationFactory.RequireConfirmation(
                    _confirmationStore, context.UserId, SkillName, parameters, reason, context.ConfirmationChannel);
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

        return CheckPersistedValues(membershipId, membership, persisted)
            ?? BuildVerifiedResult(membershipId, persisted, changed);
    }

    private static SkillResult? CheckChangeSet(Guid membershipId, MembershipResource membership, List<string> changed)
    {
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

        return null;
    }

    private static SkillResult? CheckPersistedValues(
        Guid membershipId, MembershipResource membership, MembershipResource persisted)
    {
        if (persisted.ValidFrom != membership.ValidFrom
            || persisted.ValidUntil != membership.ValidUntil)
        {
            return SkillResult.Error(
                $"Database verification failed: membership '{membershipId}' does not show the requested values after the update. " +
                "Use list_client_memberships to check the current state before retrying.");
        }

        return null;
    }

    private static SkillResult? ApplyValidFrom(
        MembershipResource membership,
        Dictionary<string, object> parameters,
        DateTime today,
        string? userLanguage,
        List<string> changed)
    {
        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var (validFrom, invalidValidFrom) = SkillDateParser.ParseOptionalUtcDate(validFromStr, today, userLanguage);
        if (invalidValidFrom)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validFrom", validFromStr!));
        }

        if (validFrom.HasValue && validFrom.Value != membership.ValidFrom)
        {
            membership.ValidFrom = validFrom.Value;
            changed.Add(ValidFromField);
        }

        return null;
    }

    private static SkillResult? ApplyValidUntil(
        MembershipResource membership,
        Dictionary<string, object> parameters,
        DateTime today,
        string? userLanguage,
        List<string> changed)
    {
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
            var validUntilStr = GetParameter<string>(parameters, "validUntil");
            var (validUntil, invalidValidUntil) = SkillDateParser.ParseOptionalUtcDate(validUntilStr, today, userLanguage);
            if (invalidValidUntil)
            {
                return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validUntil", validUntilStr!));
            }

            if (validUntil.HasValue && validUntil.Value != membership.ValidUntil)
            {
                membership.ValidUntil = validUntil.Value;
                changed.Add("validUntil");
            }
        }

        return null;
    }

    private static SkillResult BuildVerifiedResult(Guid membershipId, MembershipResource persisted, List<string> changed)
    {
        return SkillResult.SuccessResult(
            new
            {
                MembershipId = membershipId,
                ChangedFields = changed,
                persisted.ClientId,
                persisted.ValidFrom,
                persisted.ValidUntil
            },
            $"Membership updated ({string.Join(", ", changed)}) and confirmed in the database (verified). " +
            "ValidFrom is the plannability boundary in the schedule.");
    }
}
