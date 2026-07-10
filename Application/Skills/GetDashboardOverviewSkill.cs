// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows the dashboard's overview donuts as text: customers, shifts, employees and external
/// employees per group (only groups with counts, restricted to the caller's visible groups)
/// plus the totals and the dashboard visibility status (restricted / no visible groups).
/// Counts are per group assignment — a person in two groups counts twice in the totals,
/// exactly like the dashboard charts.
/// </summary>
/// <param name="limit">Optional. Maximum number of groups to list (default 20, largest first).</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_dashboard_overview")]
public class GetDashboardOverviewSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 20;

    private readonly IMediator _mediator;

    public GetDashboardOverviewSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var visibility = await _mediator.Send(new GetDashboardVisibilityStatusQuery(), cancellationToken);
        if (visibility.IsRestricted && !visibility.HasVisibleGroups)
        {
            return SkillResult.SuccessResult(
                new { visibility.IsRestricted, visibility.HasVisibleGroups },
                "The dashboard is empty for this user: the group scope is restricted and no groups are " +
                "assigned. An administrator can assign groups via set_user_group_scope.");
        }

        var tree = await _mediator.Send(
            new GetGroupTreeQuery(null, ApplyVisibilityScope: true), cancellationToken);

        var flat = new List<GroupResource>();
        Flatten(tree.Nodes, flat);

        var relevant = flat
            .Where(g => g.CustomersCount > 0 || g.ShiftsCount > 0 || g.EmployeesCount > 0 || g.ExternEmpsCount > 0)
            .OrderByDescending(g => g.CustomersCount + g.ShiftsCount + g.EmployeesCount + g.ExternEmpsCount)
            .ToList();

        var totals = new
        {
            Customers = relevant.Sum(g => g.CustomersCount),
            Shifts = relevant.Sum(g => g.ShiftsCount),
            Employees = relevant.Sum(g => g.EmployeesCount),
            ExternalEmployees = relevant.Sum(g => g.ExternEmpsCount)
        };

        var listed = relevant
            .Take(limit)
            .Select(g => new
            {
                GroupId = g.Id,
                g.Name,
                g.CustomersCount,
                g.ShiftsCount,
                g.EmployeesCount,
                g.ExternEmpsCount
            })
            .ToList();

        var truncatedNote = relevant.Count > limit
            ? $" Showing the {limit} largest of {relevant.Count} groups."
            : string.Empty;
        var restrictedNote = visibility.IsRestricted
            ? " Counts are limited to the groups visible to this user."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                visibility.IsRestricted,
                visibility.HasVisibleGroups,
                GroupsWithData = relevant.Count,
                Totals = totals,
                Groups = listed
            },
            $"Dashboard overview: {totals.Customers} customer assignment(s), {totals.Shifts} shift(s), " +
            $"{totals.Employees} employee(s) and {totals.ExternalEmployees} external(s) across " +
            $"{relevant.Count} group(s) with data.{truncatedNote}{restrictedNote}");
    }

    private static void Flatten(IEnumerable<GroupResource> nodes, List<GroupResource> into)
    {
        foreach (var node in nodes)
        {
            into.Add(node);
            if (node.Children.Count > 0)
            {
                Flatten(node.Children, into);
            }
        }
    }
}
