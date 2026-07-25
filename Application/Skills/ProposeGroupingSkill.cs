// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proposes (and on confirmation applies) a re-grouping of clients of the given entity type by location:
/// every client is assigned to the group whose name matches its address city, or — when no group name
/// matches — to the nearest group that carries coordinates, moving it from a coarse node (e.g. a canton)
/// down to the finer node (e.g. a city). Read-only: it returns a dry-run proposal (counts, per-target
/// breakdown, per-match-kind breakdown, how many existing memberships would end, a sample, and clients
/// that cannot be placed) and never persists. Always run this first and let the user confirm before
/// calling apply_grouping — a move can end further memberships, which the proposal spells out.
/// </summary>
/// <param name="entityType">Which client population to group: Employee, ExternEmp, or Customer.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Queries.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("propose_grouping")]
public class ProposeGroupingSkill : BaseSkillImplementation
{
    private const int SampleSize = 10;
    private const string NoGroups = "(none)";
    private const string EntityTypeParameterName = "entityType";

    private readonly IMediator _mediator;

    public ProposeGroupingSkill(IMediator mediator)
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
        var proposal = await _mediator.Send(new ProposeCustomerGroupingQuery(entityType), cancellationToken);

        if (proposal.AnchorGroupCount == 0)
        {
            return SkillResult.SuccessResult(
                new { proposal.AnchorGroupCount, ToMove = 0, Unassigned = proposal.Unassigned.Count },
                $"No group name matches a {noun} address city and no group has a location stored, so nothing " +
                "can be assigned. Either name the location groups after the cities, or store the location of " +
                "those groups, then ask for this proposal again.");
        }

        var byTargetGroup = proposal.Assignments
            .GroupBy(a => a.TargetGroupName)
            .Select(g => new { Group = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byMatchReason = proposal.Assignments
            .GroupBy(a => a.MatchReason)
            .Select(g => new { Match = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var unassignedByReason = proposal.Unassigned
            .GroupBy(u => u.Reason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToList();

        var membershipsToEnd = proposal.Assignments.Sum(a => a.RetireGroupNames.Count);
        var losingMembership = proposal.Assignments.Count(a => a.RetireGroupNames.Count > 0);

        var sample = proposal.Assignments
            .Take(SampleSize)
            .Select(a => new
            {
                a.ClientName,
                From = a.CurrentGroupNames.Count > 0 ? string.Join("/", a.CurrentGroupNames) : NoGroups,
                To = a.TargetGroupName,
                Ends = a.RetireGroupNames.Count > 0 ? string.Join("/", a.RetireGroupNames) : NoGroups,
                DistanceKm = a.DistanceKm.HasValue ? Math.Round(a.DistanceKm.Value, 1) : (double?)null,
                Match = a.MatchReason
            })
            .ToList();

        var data = new
        {
            EntityType = entityType.ToString(),
            IsDryRun = true,
            proposal.AnchorGroupCount,
            ToMove = proposal.Assignments.Count,
            MembershipsToEnd = membershipsToEnd,
            LosingAMembership = losingMembership,
            Unassigned = proposal.Unassigned.Count,
            ByTargetGroup = byTargetGroup,
            ByMatchReason = byMatchReason,
            UnassignedByReason = unassignedByReason,
            Sample = sample
        };

        var message =
            $"Proposal: {proposal.Assignments.Count} {noun}(s) would move to their location group " +
            $"({proposal.AnchorGroupCount} group(s) can receive {noun}s by city name or coordinates); " +
            $"{proposal.Unassigned.Count} cannot be assigned. " +
            $"{membershipsToEnd} existing group membership(s) of {losingMembership} {noun}(s) " +
            "would end as part of these moves — the sample lists them per client under 'Ends'. " +
            "Nothing has been saved yet: show these numbers to the user and wait for confirmation.";

        return SkillResult.SuccessResult(data, message);
    }
}
