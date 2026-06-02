// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proposes (and on confirmation applies) a geographic re-grouping of customers: every customer is
/// assigned to the nearest group that carries coordinates, moving it from a coarse node (e.g. a
/// canton) down to the nearest finer node (e.g. a city). With apply=false it returns a read-only
/// proposal (counts, per-target breakdown, a sample, and customers that cannot be placed); with
/// apply=true it persists the moves. Always run the proposal first and let the user confirm before
/// applying.
/// </summary>
/// <param name="apply">When true the proposal is persisted; when false (default) only a dry-run proposal is returned.</param>

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Queries.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("propose_customer_grouping")]
public class ProposeCustomerGroupingSkill : BaseSkillImplementation
{
    private const int SampleSize = 10;

    private readonly IMediator _mediator;

    public ProposeCustomerGroupingSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        if (apply)
        {
            var applyResult = await _mediator.Send(new ApplyCustomerGroupingCommand(), cancellationToken);
            return SkillResult.SuccessResult(
                applyResult,
                $"Applied: {applyResult.MovedCount} customer(s) moved to their nearest location group; " +
                $"{applyResult.UnassignedCount} could not be assigned.");
        }

        var proposal = await _mediator.Send(new ProposeCustomerGroupingQuery(), cancellationToken);

        if (proposal.AnchorGroupCount == 0)
        {
            return SkillResult.SuccessResult(
                new { proposal.AnchorGroupCount, ToMove = 0, Unassigned = proposal.Unassigned.Count },
                "No group carries coordinates yet, so nothing can be assigned. " +
                "Geocode the location groups first via set_group_location, then re-run this proposal.");
        }

        var byTargetGroup = proposal.Assignments
            .GroupBy(a => a.TargetGroupName)
            .Select(g => new { Group = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var unassignedByReason = proposal.Unassigned
            .GroupBy(u => u.Reason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToList();

        var sample = proposal.Assignments
            .Take(SampleSize)
            .Select(a => new
            {
                a.ClientName,
                From = a.CurrentGroupNames.Count > 0 ? string.Join("/", a.CurrentGroupNames) : "(none)",
                To = a.TargetGroupName,
                DistanceKm = Math.Round(a.DistanceKm, 1)
            })
            .ToList();

        var data = new
        {
            IsDryRun = true,
            proposal.AnchorGroupCount,
            ToMove = proposal.Assignments.Count,
            Unassigned = proposal.Unassigned.Count,
            ByTargetGroup = byTargetGroup,
            UnassignedByReason = unassignedByReason,
            Sample = sample
        };

        var message =
            $"Proposal: {proposal.Assignments.Count} customer(s) would move to their nearest location group " +
            $"({proposal.AnchorGroupCount} groups carry coordinates); {proposal.Unassigned.Count} cannot be assigned. " +
            "Review, then call again with apply=true to persist.";

        return SkillResult.SuccessResult(data, message);
    }
}
