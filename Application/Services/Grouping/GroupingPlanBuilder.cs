// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the grouping plan of one analysis (one instance per build). Order: F1 globally unfillable
/// shifts; F3 ungrouped (and not globally unfillable) shifts: a split or derived shift whose original has
/// groups joins all groups of the original, as ShiftCloner copies the original's group items, without any
/// eligibility ranking; else a shift with a location (its customer's address) joins the nearest located
/// planning unit whose scope has an eligible client, or the nearest located planning unit when no unit has
/// one; else the group whose scope holds most eligible clients (planning units first). A group is created
/// only when no group exists at all; F4 ungrouped clients placed into
/// the planning unit where they fit most shifts; F5 clients that fit no shift (only when at least one
/// shift is analysed); F2 greedy set cover of shifts nobody in a unit's scope can take, deepest units
/// first; F6 removal of memberships that help no shift in the group's scope and are not the client's only
/// way into an ancestor scope where the client is needed, only when another group remains and without
/// upcoming works on any shift of the group's subtree, including shifts that are not analysed because
/// they have no run day in the period. Memberships of shifts that are not in the analysed shift list are
/// otherwise ignored, so a shift without run days yields no finding. Every step reads the membership
/// state after the previous proposals, so adds are counted by later steps and removals always come after
/// all adds.
/// </summary>
/// <param name="input">Clients, shifts, tree, real memberships, upcoming works, eligibility and focus.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingPlanBuilder
{
    private readonly GroupingPlanInput _input;
    private readonly IGroupingEligibilityOracle _eligibility;
    private readonly GroupingMembershipState _state;
    private readonly IReadOnlySet<Guid>? _focus;
    private readonly List<GroupingFinding> _findings = [];
    private readonly List<GroupingProposal> _proposals = [];
    private readonly HashSet<Guid> _unfillableShifts = [];
    private readonly Dictionary<Guid, HashSet<Guid>> _allShiftsByGroup = new();
    private readonly Dictionary<Guid, List<Guid>> _allGroupsByShift = new();
    private readonly Dictionary<Guid, bool> _fitsAnyShift = new();
    private GroupingGroupTree _tree;
    private bool _newGroupProposed;

    public GroupingPlanBuilder(GroupingPlanInput input)
    {
        _input = input;
        _eligibility = input.Eligibility;
        _tree = input.Tree;
        _state = GroupingMembershipState.From(
            input.Memberships,
            input.Tree,
            input.Clients.Select(client => client.Id).ToHashSet(),
            input.Shifts.Select(shift => shift.Id).ToHashSet());
        _focus = input.FocusGroupId is Guid focus && input.Tree.Contains(focus)
            ? input.Tree.SelfAndDescendants(focus)
            : null;
        foreach (var membership in input.Memberships)
        {
            if (membership.ShiftId is Guid shiftId)
            {
                if (!_allShiftsByGroup.TryGetValue(membership.GroupId, out var shifts))
                {
                    shifts = [];
                    _allShiftsByGroup[membership.GroupId] = shifts;
                }

                shifts.Add(shiftId);
                if (!_allGroupsByShift.TryGetValue(shiftId, out var groups))
                {
                    groups = [];
                    _allGroupsByShift[shiftId] = groups;
                }

                groups.Add(membership.GroupId);
            }
        }
    }

    public GroupingPlanResult Build()
    {
        DetectGloballyUnfillableShifts();
        if (_focus is null)
        {
            PlaceUngroupedShifts();
            PlaceUngroupedClients();
        }

        DetectClientsWithoutAnyFit();
        CoverShiftsInPlanningUnits();
        RemoveDeadMemberships();

        return new GroupingPlanResult(_findings, _proposals, _tree, _state);
    }

    private void DetectGloballyUnfillableShifts()
    {
        foreach (var shift in OrderedShifts())
        {
            if (_focus is not null && !_state.GroupsOfShift(shift.Id).Any(InFocus))
            {
                continue;
            }

            if (_input.Clients.Any(client => IsEligible(client.Id, shift.Id)))
            {
                continue;
            }

            _unfillableShifts.Add(shift.Id);
            var counts = CountReasons(_input.Clients.Select(client => _eligibility.Evaluate(client.Id, shift.Id)));
            _findings.Add(new GroupingFinding(
                GroupingFindingCode.ShiftUnfillableGlobally,
                ReportOnly: true,
                ShiftId: shift.Id,
                Reason: counts.Count > 0 ? counts[0].Reason : null,
                ReasonCounts: counts));
        }
    }

    private void PlaceUngroupedShifts()
    {
        var ungrouped = OrderedShifts()
            .Where(shift => _state.GroupsOfShift(shift.Id).Count == 0 && !_unfillableShifts.Contains(shift.Id))
            .ToList();

        foreach (var shift in ungrouped)
        {
            var targets = GroupsOfOriginal(shift);
            if (targets.Count == 0)
            {
                targets = [NearestUnitForShift(shift) ?? ChooseGroupForShift(shift.Id) ?? EnsureNewGroup()];
            }

            foreach (var target in targets)
            {
                _state.AddShift(shift.Id, target);
                AddProposal(GroupingProposalKind.AddShift, target, GroupingFindingCode.ShiftWithoutGroup, shiftId: shift.Id);
                _findings.Add(new GroupingFinding(
                    GroupingFindingCode.ShiftWithoutGroup, ReportOnly: false, GroupId: RealGroupId(target), ShiftId: shift.Id));
            }
        }
    }

    private List<Guid> GroupsOfOriginal(GroupingShiftRecord shift)
    {
        if (shift.OriginalId is not Guid originalId || originalId == shift.Id
            || !_allGroupsByShift.TryGetValue(originalId, out var groups))
        {
            return [];
        }

        return groups
            .Where(_tree.Contains)
            .Distinct()
            .OrderBy(groupId => _tree.Get(groupId).Name, StringComparer.Ordinal)
            .ThenBy(groupId => groupId)
            .ToList();
    }

    private Guid? NearestUnitForShift(GroupingShiftRecord shift)
    {
        if (shift.Latitude is null || shift.Longitude is null)
        {
            return null;
        }

        var located = _state.PlanningUnits()
            .Select(unit => (Unit: unit, Kilometers: DistanceToShift(shift, unit)))
            .Where(candidate => !double.IsPositiveInfinity(candidate.Kilometers))
            .OrderBy(candidate => candidate.Kilometers)
            .ThenBy(candidate => _tree.Get(candidate.Unit).Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Unit)
            .ToList();
        if (located.Count == 0)
        {
            return null;
        }

        var hasEligibleUnit = _state.PlanningUnits().Any(unit => HasEligibleInScope(unit, shift.Id));
        if (!hasEligibleUnit)
        {
            return located[0].Unit;
        }

        return located
            .Where(candidate => HasEligibleInScope(candidate.Unit, shift.Id))
            .Select(candidate => (Guid?)candidate.Unit)
            .FirstOrDefault();
    }

    private bool HasEligibleInScope(Guid groupId, Guid shiftId) =>
        _state.ScopeClients(groupId, _tree).Any(client => IsEligible(client, shiftId));

    private double DistanceToShift(GroupingShiftRecord shift, Guid groupId)
    {
        var group = _tree.Get(groupId);
        return GroupingDistance.Kilometers(shift.Latitude, shift.Longitude, group.Latitude, group.Longitude);
    }

    private Guid? ChooseGroupForShift(Guid shiftId)
    {
        var candidates = _tree.GroupIds
            .Select(groupId =>
            {
                var scope = _state.ScopeClients(groupId, _tree);
                return (GroupId: groupId, Eligible: scope.Count(client => IsEligible(client, shiftId)), Size: scope.Count);
            })
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var withEligible = candidates
            .Where(candidate => candidate.Eligible > 0)
            .OrderByDescending(candidate => _state.IsPlanningUnit(candidate.GroupId))
            .ThenByDescending(candidate => candidate.Eligible)
            .ThenBy(candidate => _tree.Get(candidate.GroupId).Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.GroupId)
            .ToList();
        if (withEligible.Count > 0)
        {
            return withEligible[0].GroupId;
        }

        return candidates
            .OrderByDescending(candidate => _state.IsPlanningUnit(candidate.GroupId))
            .ThenByDescending(candidate => candidate.Size)
            .ThenBy(candidate => _tree.Get(candidate.GroupId).Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.GroupId)
            .First().GroupId;
    }

    private Guid EnsureNewGroup()
    {
        var placeholder = GroupingFeasibilityDefaults.NewGroupPlaceholderId;
        if (_newGroupProposed)
        {
            return placeholder;
        }

        _tree = _tree.WithGroup(new GroupingGroupRecord(
            placeholder, GroupingFeasibilityDefaults.NewGroupKey, null, null, null, null));
        _proposals.Add(new GroupingProposal(
            GroupingProposalKind.CreateGroup, null, GroupingFeasibilityDefaults.NewGroupKey, null, null,
            GroupingFindingCode.ShiftWithoutGroup));
        _newGroupProposed = true;
        return placeholder;
    }

    private void PlaceUngroupedClients()
    {
        var ungrouped = OrderedClients().Where(client => _state.GroupsOfClient(client.Id).Count == 0).ToList();
        foreach (var client in ungrouped)
        {
            var best = _state.PlanningUnits()
                .Select(groupId => (GroupId: groupId, Fit: _state.ScopeShifts(groupId, _tree).Count(shift => IsEligible(client.Id, shift))))
                .Where(candidate => candidate.Fit > 0)
                .OrderByDescending(candidate => candidate.Fit)
                .ThenBy(candidate => Distance(client, candidate.GroupId))
                .ThenBy(candidate => _tree.Get(candidate.GroupId).Name, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.GroupId)
                .Select(candidate => (Guid?)candidate.GroupId)
                .FirstOrDefault();

            if (best is Guid target)
            {
                _state.AddClient(client.Id, target);
                AddProposal(GroupingProposalKind.AddClient, target, GroupingFindingCode.ClientWithoutGroup, clientId: client.Id);
                _findings.Add(new GroupingFinding(
                    GroupingFindingCode.ClientWithoutGroup, ReportOnly: false, GroupId: RealGroupId(target), ClientId: client.Id));
            }
        }
    }

    private void DetectClientsWithoutAnyFit()
    {
        if (_input.Shifts.Count == 0)
        {
            return;
        }

        foreach (var client in OrderedClients())
        {
            if (_focus is not null && !_state.GroupsOfClient(client.Id).Any(InFocus))
            {
                continue;
            }

            if (FitsAnyShift(client.Id))
            {
                continue;
            }

            var counts = CountReasons(_input.Shifts.Select(shift => _eligibility.Evaluate(client.Id, shift.Id)));
            _findings.Add(new GroupingFinding(
                GroupingFindingCode.ClientFitsNoShift,
                ReportOnly: true,
                ClientId: client.Id,
                Reason: counts.Count > 0 ? counts[0].Reason : null,
                ReasonCounts: counts));
        }
    }

    private void CoverShiftsInPlanningUnits()
    {
        var units = _state.PlanningUnits()
            .Where(InFocus)
            .OrderByDescending(groupId => _tree.Depth(groupId))
            .ThenBy(groupId => _tree.Get(groupId).Name, StringComparer.Ordinal)
            .ThenBy(groupId => groupId)
            .ToList();

        foreach (var unit in units)
        {
            var scopeClients = _state.ScopeClients(unit, _tree).ToHashSet();
            var uncovered = _state.ScopeShifts(unit, _tree)
                .Where(shift => !_unfillableShifts.Contains(shift) && !scopeClients.Any(client => IsEligible(client, shift)))
                .ToHashSet();
            var covered = new HashSet<Guid>();

            while (uncovered.Count > 0)
            {
                var pick = PickCoveringClient(unit, scopeClients, uncovered);
                if (pick is not Guid clientId)
                {
                    break;
                }

                _state.AddClient(clientId, unit);
                scopeClients.Add(clientId);
                AddProposal(GroupingProposalKind.AddClient, unit, GroupingFindingCode.ShiftUncoveredInGroup, clientId: clientId);
                var newlyCovered = uncovered.Where(shift => IsEligible(clientId, shift)).ToList();
                covered.UnionWith(newlyCovered);
                uncovered.ExceptWith(newlyCovered);
            }

            foreach (var shiftId in covered.OrderBy(id => id))
            {
                _findings.Add(new GroupingFinding(
                    GroupingFindingCode.ShiftUncoveredInGroup, ReportOnly: false, GroupId: RealGroupId(unit), ShiftId: shiftId));
            }
        }
    }

    private Guid? PickCoveringClient(Guid unit, IReadOnlySet<Guid> scopeClients, IReadOnlySet<Guid> uncovered) =>
        OrderedClients()
            .Where(client => !scopeClients.Contains(client.Id) && FitsAnyShift(client.Id))
            .Select(client => (Client: client, Cover: uncovered.Count(shift => IsEligible(client.Id, shift))))
            .Where(candidate => candidate.Cover > 0)
            .OrderByDescending(candidate => candidate.Cover)
            .ThenBy(candidate => _state.GroupsOfClient(candidate.Client.Id).Count)
            .ThenBy(candidate => Distance(candidate.Client, unit))
            .ThenBy(candidate => candidate.Client.DisplayName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Client.Id)
            .Select(candidate => (Guid?)candidate.Client.Id)
            .FirstOrDefault();

    private void RemoveDeadMemberships()
    {
        var knownClients = _input.Clients.Select(client => client.Id).ToHashSet();
        var memberships = _input.Memberships
            .Where(m => m.ClientId is Guid clientId && knownClients.Contains(clientId) && _tree.Contains(m.GroupId) && InFocus(m.GroupId))
            .Select(m => (ClientId: m.ClientId!.Value, m.GroupId))
            .Distinct()
            .OrderBy(m => m.GroupId)
            .ThenBy(m => m.ClientId)
            .ToList();

        foreach (var (clientId, groupId) in memberships)
        {
            if (!_state.GroupsOfClient(clientId).Contains(groupId))
            {
                continue;
            }

            var scopeShifts = _state.ScopeShifts(groupId, _tree);
            if (scopeShifts.Count == 0)
            {
                continue;
            }

            if (scopeShifts.Any(shift => IsEligible(clientId, shift)) || IsNeededViaAncestor(clientId, groupId))
            {
                continue;
            }

            var hasUpcomingWork = HasUpcomingWorkInSubtree(clientId, groupId, scopeShifts);
            var keepsAnotherGroup = _state.GroupsOfClient(clientId).Any(other => other != groupId);
            if (!hasUpcomingWork && keepsAnotherGroup)
            {
                _state.RemoveClient(clientId, groupId);
                AddProposal(GroupingProposalKind.RemoveClient, groupId, GroupingFindingCode.ClientDeadMembership, clientId: clientId);
                _findings.Add(new GroupingFinding(GroupingFindingCode.ClientDeadMembership, ReportOnly: false, GroupId: groupId, ClientId: clientId));
            }
            else
            {
                _findings.Add(new GroupingFinding(GroupingFindingCode.ClientDeadMembership, ReportOnly: true, GroupId: groupId, ClientId: clientId));
            }
        }
    }

    private bool HasUpcomingWorkInSubtree(Guid clientId, Guid groupId, IReadOnlySet<Guid> scopeShifts) =>
        scopeShifts
            .Concat(_tree.SelfAndDescendants(groupId).SelectMany(member => _allShiftsByGroup.GetValueOrDefault(member) ?? []))
            .Any(shift => _input.FutureWorkPairs.Contains(new GroupingEntityPair(clientId, shift)));

    private bool IsNeededViaAncestor(Guid clientId, Guid groupId) =>
        _tree.SelfAndAncestors(groupId)
            .Skip(1)
            .Any(ancestor =>
            {
                var ancestorSubtree = _tree.SelfAndDescendants(ancestor);
                var keptInAncestorScopeAnyway = _state.GroupsOfClient(clientId)
                    .Any(other => other != groupId && ancestorSubtree.Contains(other));
                return !keptInAncestorScopeAnyway
                    && _state.ScopeShifts(ancestor, _tree).Any(shift => IsEligible(clientId, shift));
            });

    private void AddProposal(GroupingProposalKind kind, Guid target, GroupingFindingCode cause, Guid? clientId = null, Guid? shiftId = null)
    {
        var isNewGroup = target == GroupingFeasibilityDefaults.NewGroupPlaceholderId;
        _proposals.Add(new GroupingProposal(
            kind,
            isNewGroup ? null : target,
            isNewGroup ? GroupingFeasibilityDefaults.NewGroupKey : null,
            clientId,
            shiftId,
            cause));
    }

    private static Guid? RealGroupId(Guid groupId) =>
        groupId == GroupingFeasibilityDefaults.NewGroupPlaceholderId ? null : groupId;

    private static IReadOnlyList<GroupingReasonCount> CountReasons(IEnumerable<EligibilityVerdict> verdicts) =>
        verdicts
            .Where(verdict => verdict.Reason is not null)
            .GroupBy(verdict => verdict.Reason!.Value)
            .Select(group => new GroupingReasonCount(group.Key, group.Count()))
            .OrderByDescending(count => count.Count)
            .ThenBy(count => count.Reason)
            .ToList();

    private bool FitsAnyShift(Guid clientId)
    {
        if (!_fitsAnyShift.TryGetValue(clientId, out var fits))
        {
            fits = _input.Shifts.Any(shift => IsEligible(clientId, shift.Id));
            _fitsAnyShift[clientId] = fits;
        }

        return fits;
    }

    private bool InFocus(Guid groupId) => _focus is null || _focus.Contains(groupId);

    private bool IsEligible(Guid clientId, Guid shiftId) => _eligibility.IsEligible(clientId, shiftId);

    private double Distance(GroupingClientRecord client, Guid groupId)
    {
        var group = _tree.Get(groupId);
        return GroupingDistance.Kilometers(client.Latitude, client.Longitude, group.Latitude, group.Longitude);
    }

    private IEnumerable<GroupingShiftRecord> OrderedShifts() =>
        _input.Shifts.OrderBy(shift => shift.DisplayName, StringComparer.Ordinal).ThenBy(shift => shift.Id);

    private IEnumerable<GroupingClientRecord> OrderedClients() =>
        _input.Clients.OrderBy(client => client.DisplayName, StringComparer.Ordinal).ThenBy(client => client.Id);
}
