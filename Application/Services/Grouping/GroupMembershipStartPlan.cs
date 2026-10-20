// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Start dates of new group memberships for a set of persons. In Fixed mode everybody starts on the same day; in
/// EntryDate mode each person starts on the day of their own employment entry (Membership.ValidFrom) and a person
/// without a membership row falls back to the fallback day. Describe turns the plan into the structured facts a
/// grouping skill reports: the start date, how many persons entered earlier and are still employed at the start (the
/// schedule shows their days between entry and group start blue and locked; a person who left before the start is not
/// counted) and, in EntryDate mode, how many existing members hold a start after their entry. Dates are UTC midnight values.
/// </summary>
/// <param name="mode">Fixed or EntryDate</param>
/// <param name="startDateGiven">False when no start date was supplied and the default applies</param>
/// <param name="fixedStart">The common start in Fixed mode (supplied or default); ignored in EntryDate mode</param>
/// <param name="fallbackStart">Start of a person without an entry date in EntryDate mode (the company today)</param>
/// <param name="windows">Employment window per client; a client without a membership row has no entry</param>

using System.Globalization;
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

    public GroupMembershipStartPlan(
        GroupMembershipStartMode mode,
        bool startDateGiven,
        DateTime fixedStart,
        DateTime fallbackStart,
        IReadOnlyDictionary<Guid, MembershipWindow> windows)
    {
        _mode = mode;
        _startDateGiven = startDateGiven;
        _fixedStart = fixedStart;
        _fallbackStart = fallbackStart;
        _windows = windows;
    }

    public GroupMembershipStartMode Mode => _mode;

    public DateTime StartFor(Guid clientId)
    {
        if (_mode == GroupMembershipStartMode.Fixed)
        {
            return _fixedStart;
        }

        return _windows.TryGetValue(clientId, out var window) ? ToUtcMidnight(window.ValidFrom) : _fallbackStart;
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
        var existing = ExistingStartingAfterEntry(existingMembers);

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
            existing.Count == 0 ? null : existing.Min().ToString(IsoDate, CultureInfo.InvariantCulture));
    }

    private List<DateOnly> ExistingStartingAfterEntry(IEnumerable<ExistingMembershipStart>? existingMembers)
    {
        if (_mode != GroupMembershipStartMode.EntryDate || existingMembers is null)
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

    private static bool EnteredBeforeAndStillEmployed(MembershipWindow window, DateTime start) =>
        ToUtcMidnight(window.ValidFrom) < start
        && (window.ValidUntil is null || ToUtcMidnight(window.ValidUntil.Value) >= start);

    private static DateTime ToUtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static string Format(DateTime value) => value.ToString(IsoDate, CultureInfo.InvariantCulture);
}
