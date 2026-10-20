// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// <param name="validFrom">Optional start date of the new memberships as yyyy-MM-dd or 'today', or 'entry' to start each person on their own employment entry date; omitted means the company's current date. An unparseable value is rejected rather than silently replaced by today, because a wrong membership start is invisible in the answer.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("apply_grouping")]
public class ApplyGroupingSkill : BaseSkillImplementation
{
    private const string EntityTypeParameterName = "entityType";

    private const string ValidFromParameterName = "validFrom";

    private const string ValidFromFormat = "yyyy-MM-dd";

    private const string UnstructuredStartTemplate =
        "The new memberships start on {0} — state this date in the answer.";

    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public ApplyGroupingSkill(IMediator mediator, ICompanyClock companyClock)
    {
        _mediator = mediator;
        _companyClock = companyClock;
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

        var validFromValue = GetParameter<string>(parameters, ValidFromParameterName);
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, startMode, invalidDate) = SkillDateParser.ParseMembershipStart(validFromValue, today, context.UserLanguage);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor(ValidFromParameterName, validFromValue!));
        }

        CustomerGroupingApplyResult result;
        try
        {
            result = await _mediator.Send(
                new ApplyCustomerGroupingCommand(entityType, validFrom, startMode), cancellationToken);
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
            $"{result.UnassignedCount} could not be assigned. " +
            (result.MembershipStart is null
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    UnstructuredStartTemplate,
                    result.AppliedValidFrom.ToString(ValidFromFormat, CultureInfo.InvariantCulture))
                : GroupMembershipStartHint.For(result.MembershipStart, applied: true)));
    }
}
