// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Start dates of new group memberships for a set of persons. In Fixed mode everybody starts on the same day; in
/// EntryDate mode each person starts on the day of their own employment entry (Membership.ValidFrom) and a person
/// without a usable entry date falls back to the fallback day. An entry date that fails MembershipValidFromPlausibility
/// (placeholders such as 0001-01-01 or 1900-01-01) is never used - not for a new membership, not to pull an existing one
/// forward - and is counted separately. Describe turns the plan into the structured facts a grouping skill reports: the
/// start date, how many persons entered earlier and are still employed at the start (the schedule shows their days
/// between entry and group start blue and locked; a person who left before the start is not counted) and, in EntryDate
/// mode, how many existing memberships of the target groups would be or were pulled forward, how many were not (ended
/// memberships, or a start that would reach back before an earlier group ended) and how many existing members outside
/// the target groups start after their entry. Dates are UTC midnight values.
/// </summary>
/// <param name="mode">Fixed or EntryDate</param>
/// <param name="startDateGiven">False when no start date was supplied and the default applies</param>
/// <param name="fixedStart">The common start in Fixed mode (supplied or default); ignored in EntryDate mode</param>
/// <param name="fallbackStart">Start of a person without a usable entry date in EntryDate mode, and the reference day of the plausibility check (the company today)</param>
/// <param name="windows">Employment window per client; a client without a membership row has no entry</param>
/// <param name="endedMemberships">Ended memberships (GroupItem with ValidUntil) per client with their group; read only in EntryDate mode</param>

using System.Globalization;
using Klacks.Api.Application.Common;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupMembershipStartPlan
{
    private const string IsoDate = "yyyy-MM-dd";

    private readonly GroupMembershipStartMode _mode;
    private readonly bool _startDateGiven;
    private readonly DateTime _fixedStart;
    private readonly DateTime _fallbackStart;
    private readonly IReadOnlyDictionary<Guid, MembershipWindow> _windows;
    private readonly ILookup<Guid, (Guid GroupId, DateTime ValidUntil)> _endedMemberships;

    public GroupMembershipStartPlan(
        GroupMembershipStartMode mode,
        bool startDateGiven,
        DateTime fixedStart,
        DateTime fallbackStart,
        IReadOnlyDictionary<Guid, MembershipWindow> windows,
        ILookup<Guid, (Guid GroupId, DateTime ValidUntil)>? endedMemberships = null)
    {
        _mode = mode;
        _startDateGiven = startDateGiven;
        _fixedStart = fixedStart;
        _fallbackStart = fallbackStart;
        _windows = windows;
        _endedMemberships = endedMemberships
            ?? Array.Empty<(Guid ClientId, Guid GroupId, DateTime ValidUntil)>()
                .ToLookup(entry => entry.ClientId, entry => (entry.GroupId, entry.ValidUntil));
    }

    public GroupMembershipStartMode Mode => _mode;

    public DateTime Today => _fallbackStart;

    public DateTime StartFor(Guid clientId)
    {
        if (_mode == GroupMembershipStartMode.Fixed)
        {
            return _fixedStart;
        }

        return _windows.TryGetValue(clientId, out var window) && IsPlausible(window)
            ? ToUtcMidnight(window.ValidFrom)
            : _fallbackStart;
    }

    public DateTime GroupStart(IEnumerable<Guid> memberIds)
    {
        if (_mode == GroupMembershipStartMode.Fixed)
        {
            return _fixedStart;
        }

        var earliest = memberIds.Select(StartFor).DefaultIfEmpty(_fallbackStart).Min();
        return earliest < _fallbackStart ? earliest : _fallbackStart;
    }

    public GroupMembershipStartInfo Describe(
        IEnumerable<Guid> clientIds, IEnumerable<ExistingMembershipStart>? existingMembers = null)
    {
        var persons = clientIds.Distinct().ToList();
        var starts = persons.Select(id => (Id: id, Start: StartFor(id))).ToList();
        var withoutEntry = persons.Count(id => !_windows.ContainsKey(id));
        var enteredEarlier = starts
            .Where(s => _windows.TryGetValue(s.Id, out var window) && EnteredBeforeAndStillEmployed(window, s.Start))
            .Select(s => _windows[s.Id].ValidFrom)
            .ToList();
        var existingList = existingMembers?.ToList() ?? [];
        var (pullable, notPulledForward) = AnalyzePullForward(existingList);
        var existing = ExistingStartingAfterEntry(existingList.Where(member => member.ItemId is null || member.GroupId is null));
        var implausible = persons.Concat(existingList.Select(member => member.ClientId)).Distinct()
            .Count(id => _windows.TryGetValue(id, out var window) && !IsPlausible(window));

        return new GroupMembershipStartInfo(
            _mode,
            _startDateGiven,
            _mode == GroupMembershipStartMode.Fixed ? Format(_fixedStart) : null,
            starts.Count == 0 ? null : Format(starts.Min(s => s.Start)),
            starts.Count == 0 ? null : Format(starts.Max(s => s.Start)),
            persons.Count,
            enteredEarlier.Count,
            withoutEntry,
            enteredEarlier.Count == 0 ? null : enteredEarlier.Min().ToString(IsoDate, CultureInfo.InvariantCulture),
            existing.Count,
            existing.Count == 0 ? null : existing.Min().ToString(IsoDate, CultureInfo.InvariantCulture),
            pullable.Count,
            pullable.Count == 0 ? null : Format(pullable.Min(candidate => candidate.NewStart)),
            notPulledForward,
            implausible);
    }

    public IReadOnlyList<PullForwardCandidate> PullForwardCandidates(IEnumerable<ExistingMembershipStart>? existingMembers) =>
        AnalyzePullForward(existingMembers).Candidates;

    private (IReadOnlyList<PullForwardCandidate> Candidates, int NotPulledForward) AnalyzePullForward(
        IEnumerable<ExistingMembershipStart>? existingMembers)
    {
        if (_mode != GroupMembershipStartMode.EntryDate || existingMembers is null)
        {
            return ([], 0);
        }

        var candidates = new Dictionary<Guid, PullForwardCandidate>();
        var notPulledForward = new HashSet<Guid>();
        foreach (var member in existingMembers)
        {
            if (member.ItemId is not { } itemId
                || member.GroupId is not { } groupId
                || member.ValidFrom is not { } validFrom
                || !_windows.TryGetValue(member.ClientId, out var window))
            {
                continue;
            }

            var currentStart = ToUtcMidnight(DateOnly.FromDateTime(validFrom));
            if (!EnteredBeforeAndStillEmployed(window, currentStart))
            {
                continue;
            }

            var newStart = member.ValidUntil is null ? NewStartAfterEarlierGroups(member.ClientId, groupId, window) : currentStart;
            if (newStart >= currentStart)
            {
                notPulledForward.Add(itemId);
                continue;
            }

            candidates[itemId] = new PullForwardCandidate(itemId, groupId, member.ClientId, newStart);
        }

        return (candidates.Values.ToList(), notPulledForward.Count);
    }

    private DateTime NewStartAfterEarlierGroups(Guid clientId, Guid groupId, MembershipWindow window)
    {
        var entry = ToUtcMidnight(window.ValidFrom);
        var endedElsewhere = _endedMemberships[clientId].Where(ended => ended.GroupId != groupId).Select(ended => ended.ValidUntil).ToList();
        if (endedElsewhere.Count == 0)
        {
            return entry;
        }

        var dayAfter = ToUtcMidnight(DateOnly.FromDateTime(endedElsewhere.Max())).AddDays(1);
        return dayAfter > entry ? dayAfter : entry;
    }

    private List<DateOnly> ExistingStartingAfterEntry(IEnumerable<ExistingMembershipStart> existingMembers)
    {
        if (_mode != GroupMembershipStartMode.EntryDate)
        {
            return [];
        }

        var entries = new List<DateOnly>();
        foreach (var person in existingMembers.GroupBy(member => member.ClientId))
        {
            if (!_windows.TryGetValue(person.Key, out var window) || person.Any(member => member.ValidFrom is null))
            {
                continue;
            }

            var start = ToUtcMidnight(DateOnly.FromDateTime(person.Min(member => member.ValidFrom!.Value)));
            if (EnteredBeforeAndStillEmployed(window, start))
            {
                entries.Add(window.ValidFrom);
            }
        }

        return entries;
    }

    private bool IsPlausible(MembershipWindow window) =>
        MembershipValidFromPlausibility.Evaluate(ToUtcMidnight(window.ValidFrom), null, _fallbackStart) is null;

    private bool EnteredBeforeAndStillEmployed(MembershipWindow window, DateTime start) =>
        IsPlausible(window)
        && ToUtcMidnight(window.ValidFrom) < start
        && (window.ValidUntil is null || ToUtcMidnight(window.ValidUntil.Value) >= start);

    private static DateTime ToUtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static string Format(DateTime value) => value.ToString(IsoDate, CultureInfo.InvariantCulture);
}
