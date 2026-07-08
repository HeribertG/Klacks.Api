// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assigns employees who currently belong to no group to the group whose name exactly matches their
/// address city (e.g. an employee living in Bern joins the group "Bern"). With apply=false (default) it
/// returns a read-only preview of the planned assignments plus the cities that have no matching group and
/// the count of employees without an address; with apply=true it persists and verifies the memberships.
/// </summary>
/// <param name="count">Optional maximum number of employees to assign.</param>
/// <param name="validFrom">Start date of the new memberships; required when apply=true.</param>
/// <param name="apply">When true the assignments are persisted; when false (default) only a preview is returned.</param>

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("group_ungrouped_by_city_name")]
public class GroupUngroupedByCityNameSkill : BaseSkillImplementation
{
    private const int MaxPreviewNames = 15;
    private const int MaxUnmatchedCities = 10;
    private const string RestrictedScopeError =
        "This skill assigns employees to city-named groups across the whole group tree and is only " +
        "available to users with unrestricted group scope. Your scope is limited to: {0}. " +
        "Add the employees to groups inside your scope individually instead.";

    private readonly IMediator _mediator;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICompanyClock _companyClock;

    public GroupUngroupedByCityNameSkill(
        IMediator mediator, IGroupScopeGuard groupScopeGuard, ICompanyClock companyClock)
    {
        _mediator = mediator;
        _groupScopeGuard = groupScopeGuard;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var count = GetParameter<int?>(parameters, "count");
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsUnrestricted)
        {
            return SkillResult.Error(
                string.Format(RestrictedScopeError, string.Join(", ", scope.VisibleRootNames)));
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"), today);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        if (apply && validFrom is null)
        {
            return SkillResult.Error(
                SkillDateParser.MissingStartDateMessage(
                    "assign the ungrouped employees to their city-named groups"));
        }

        GroupUngroupedByCityNameResult result;
        try
        {
            result = await _mediator.Send(
                new GroupUngroupedByCityNameCommand(count, validFrom, apply, context.UserName),
                cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (result.MatchCount == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"None of the {result.TotalUngrouped} ungrouped employee(s) could be matched to a group by " +
                $"city name. {BuildDiagnostics(result)} " +
                "Do not invent groups — offer to create the missing group(s) or check the addresses.");
        }

        var names = string.Join(", ",
            result.Assignments.Take(MaxPreviewNames).Select(a => $"{a.ClientName} → {a.GroupName}"));
        var more = result.Assignments.Count > MaxPreviewNames
            ? $" (+{result.Assignments.Count - MaxPreviewNames} more)"
            : string.Empty;

        if (!apply)
        {
            var validFromAsk = validFrom is null ? SkillDateParser.AskForStartDateInPreview : string.Empty;
            return SkillResult.SuccessResult(
                result,
                $"Preview: {result.MatchCount} of {result.TotalUngrouped} ungrouped employee(s) would be " +
                $"assigned to the group matching their city: {names}{more}. {BuildDiagnostics(result)} " +
                $"Nothing was changed yet.{validFromAsk} Ask the user to confirm, then call again with apply=true.");
        }

        return SkillResult.SuccessResult(
            result,
            $"Assigned {result.AddedCount} employee(s) to their city-named group and confirmed " +
            $"{result.VerifiedCount} in the database (verified). {BuildDiagnostics(result)}");
    }

    private static string BuildDiagnostics(GroupUngroupedByCityNameResult result)
    {
        var parts = new List<string>();

        if (result.NoAddressCount > 0)
        {
            parts.Add($"{result.NoAddressCount} employee(s) have no address and were skipped");
        }

        if (result.UnmatchedCities.Count > 0)
        {
            var cities = string.Join(", ",
                result.UnmatchedCities.Take(MaxUnmatchedCities).Select(c => $"{c.City} ({c.EmployeeCount})"));
            var more = result.UnmatchedCities.Count > MaxUnmatchedCities
                ? $" and {result.UnmatchedCities.Count - MaxUnmatchedCities} more"
                : string.Empty;
            parts.Add($"cities with no matching group: {cities}{more}");
        }

        return parts.Count > 0 ? string.Join("; ", parts) + "." : string.Empty;
    }
}
