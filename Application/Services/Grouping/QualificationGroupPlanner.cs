// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure, read-only planner for partition_clients_by_qualification: one group per qualification held today by at
/// least the minimum number of considered clients, named after the qualification in the installation language.
/// A client holding several qualifications joins several groups; existing memberships are never ended. A group
/// with the same name under the same parent is reused, and only clients that are not yet current members of it
/// count as new members, so a re-run is idempotent. A qualification is valid today when its row is not deleted and
/// ValidFrom/ValidUntil (each optional) cover today — the predicate evaluate_grouping_by_qualification and
/// fill_group_by_criteria use. A membership is current when it is not deleted, not a scenario row and has not
/// ended before today. With includeAlreadyGrouped=false a client counts as already grouped only through memberships
/// outside the parent's own subtree, so the run's own groups never exclude their members. The command handler is
/// the only place that writes.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Grouping;

public static class QualificationGroupPlanner
{
    private const string MissingMasterWarning =
        "{0} client qualification(s) point to a qualification that no longer exists; they were ignored.";

    private const string UnnamedQualificationWarning =
        "{0} client qualification(s) point to a qualification without a name in the installation language or a core language; they were ignored.";

    private const string MergedNameWarning =
        "Several qualifications are named '{0}' in the installation language; their holders share one group.";

    private const string AmbiguousExistingWarning =
        "Several groups named '{0}' already exist under the same parent; the first one (id {1}) is reused.";

    /// <summary>
    /// Plans the qualification groups and their members.
    /// </summary>
    /// <param name="clients">Clients of the requested types with qualifications and group memberships loaded</param>
    /// <param name="qualifications">Qualification master records, for the group names</param>
    /// <param name="existingGroups">All groups currently in the database</param>
    /// <param name="context">Name languages, today, parent, scope, minimum and the already-grouped switch</param>
    public static QualificationGroupPlan Plan(
        IReadOnlyList<Client> clients,
        IReadOnlyList<Qualification> qualifications,
        IReadOnlyList<Group> existingGroups,
        QualificationGroupPlanContext context)
    {
        var activeQualificationIds = qualifications.Where(q => !q.IsDeleted).Select(q => q.Id).ToHashSet();
        var nameByQualificationId = qualifications
            .Where(q => !q.IsDeleted)
            .Select(q => (q.Id, Name: ResolveName(q, context.NameLanguages)))
            .Where(q => !string.IsNullOrWhiteSpace(q.Name))
            .GroupBy(q => q.Id)
            .ToDictionary(g => g.Key, g => g.First().Name!);

        var considered = 0;
        var skippedAlreadyGrouped = 0;
        var withoutQualification = 0;
        var missingMaster = 0;
        var unnamed = 0;
        var membersByName = new Dictionary<string, SortedSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        var qualificationIdsByName = new Dictionary<string, SortedSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        var displayNameByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var client in clients)
        {
            var currentMemberships = client.GroupItems.Where(gi => IsCurrentMembership(gi, context.Today)).ToList();
            if (context.ScopeGroupIds != null && !currentMemberships.Any(gi => context.ScopeGroupIds.Contains(gi.GroupId)))
            {
                continue;
            }

            considered++;

            if (!context.IncludeAlreadyGrouped
                && currentMemberships.Any(gi => context.OwnSubtreeGroupIds == null || !context.OwnSubtreeGroupIds.Contains(gi.GroupId)))
            {
                skippedAlreadyGrouped++;
                continue;
            }

            var validQualificationIds = client.Qualifications
                .Where(q => IsValidToday(q, context.Today))
                .Select(q => q.QualificationId)
                .Distinct()
                .ToList();

            if (validQualificationIds.Count == 0)
            {
                withoutQualification++;
                continue;
            }

            foreach (var qualificationId in validQualificationIds)
            {
                if (!nameByQualificationId.TryGetValue(qualificationId, out var name))
                {
                    if (activeQualificationIds.Contains(qualificationId))
                    {
                        unnamed++;
                    }
                    else
                    {
                        missingMaster++;
                    }

                    continue;
                }

                var key = name.Trim();
                if (!membersByName.TryGetValue(key, out var members))
                {
                    members = new SortedSet<Guid>();
                    membersByName[key] = members;
                    qualificationIdsByName[key] = new SortedSet<Guid>();
                    displayNameByName[key] = key;
                }

                members.Add(client.Id);
                qualificationIdsByName[key].Add(qualificationId);
            }
        }

        var warnings = new List<string>();
        if (missingMaster > 0)
        {
            warnings.Add(string.Format(MissingMasterWarning, missingMaster));
        }

        if (unnamed > 0)
        {
            warnings.Add(string.Format(UnnamedQualificationWarning, unnamed));
        }

        var groupsUnderParent = context.ParentGroupId is { } parentId
            ? existingGroups
                .Where(g => !g.IsDeleted && g.Parent == parentId && !string.IsNullOrWhiteSpace(g.Name))
                .ToLookup(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            : Enumerable.Empty<Group>().ToLookup(g => g.Name, StringComparer.OrdinalIgnoreCase);

        var clientById = clients.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
        var planned = new List<PlannedQualificationGroup>();
        var skipped = new List<SkippedQualificationGroup>();

        foreach (var name in membersByName.Keys.OrderBy(n => displayNameByName[n], StringComparer.Ordinal))
        {
            var displayName = displayNameByName[name];
            var members = membersByName[name].ToList();
            var qualificationIds = qualificationIdsByName[name].ToList();

            if (qualificationIds.Count > 1)
            {
                warnings.Add(string.Format(MergedNameWarning, displayName));
            }

            if (members.Count < context.MinMembers)
            {
                skipped.Add(new SkippedQualificationGroup(displayName, members.Count));
                continue;
            }

            var candidates = groupsUnderParent[name].OrderBy(g => g.Id).ToList();
            var existing = candidates.FirstOrDefault();
            if (candidates.Count > 1)
            {
                warnings.Add(string.Format(AmbiguousExistingWarning, displayName, existing!.Id));
            }

            var newMembers = existing == null
                ? members
                : members.Where(id => !IsCurrentMemberOf(clientById[id], existing.Id, context.Today)).ToList();

            planned.Add(new PlannedQualificationGroup(
                existing?.Name ?? displayName, qualificationIds, existing != null, existing?.Id, members, newMembers));
        }

        return new QualificationGroupPlan(
            clients.Count, considered, skippedAlreadyGrouped, withoutQualification, planned, skipped, warnings);
    }

    /// <summary>
    /// Resolves the display name of a qualification in the first language of the list that carries one.
    /// </summary>
    /// <param name="qualification">The qualification master record</param>
    /// <param name="languages">Languages in order of preference</param>
    public static string? ResolveName(Qualification qualification, IReadOnlyList<string> languages) =>
        languages
            .Select(language => qualification.Name?.GetValue(language))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();

    /// <summary>
    /// True when the membership is not deleted, not a scenario row and has not ended before today.
    /// </summary>
    /// <param name="item">The membership</param>
    /// <param name="today">Company-local date</param>
    public static bool IsCurrentMembership(GroupItem item, DateOnly today) =>
        !item.IsDeleted
        && item.AnalyseToken == null
        && (item.ValidUntil == null || DateOnly.FromDateTime(item.ValidUntil.Value) >= today);

    private static bool IsValidToday(ClientQualification qualification, DateOnly today) =>
        !qualification.IsDeleted
        && (qualification.ValidFrom == null || qualification.ValidFrom <= today)
        && (qualification.ValidUntil == null || qualification.ValidUntil >= today);

    private static bool IsCurrentMemberOf(Client client, Guid groupId, DateOnly today) =>
        client.GroupItems.Any(gi => gi.GroupId == groupId && IsCurrentMembership(gi, today));
}
