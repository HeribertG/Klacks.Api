// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that says which group items of the real plan a reader means. A regular membership is a group item
/// without scenario token that is not a temporary membership of a cover (GroupItem.IsTemporary): the master data of
/// employees and groups, group mail, member lists and group-wide writes read through IsRegular, so the days a person
/// was borrowed for show up neither there nor in what a save of that master data changes. A borrowed person belongs
/// to the receiving group only on the borrowed days and only for planning: the readers of the plan (day locks, absence
/// scope, payroll completeness) take the temporary rows as well (OrTemporary); whether and how they test the day is
/// their own choice, the predicate tests none. The period closing, which has only a period, takes the temporary rows
/// that touch it (OrTemporaryWithin). The person list of the schedule grid does not read through this class:
/// ClientGroupFilterService.FilterClientsByGroupId tests the token by hand and lists a borrowed person in the receiving
/// group without testing a day, and the planning runs of that group take her out of their agents again
/// (IBorrowedAgentFilter).
/// IsRegular is the expression for queries, Holds the same test on a loaded row; OfClient, ItemsOfGroup and
/// MembersOfGroup are the filtered navigations for an Include and ClientHasOne the filter on a client query, all built
/// from IsRegular, so the rule is written
/// once. GroupItemMembershipPredicateGuardTests keeps a client-membership query on the group items from writing the
/// token test by hand.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Groups;

public static class RegularMembership
{
    private const string OwnerParameterName = "owner";

    public static readonly Expression<Func<GroupItem, bool>> IsRegular =
        item => item.AnalyseToken == null && !item.IsTemporary;

    /// <summary>
    /// Every group item of the real plan, the temporary memberships of borrowed days included. For readers that test
    /// the validity of each row against a day themselves.
    /// </summary>
    public static readonly Expression<Func<GroupItem, bool>> OrTemporary =
        item => item.AnalyseToken == null;

    /// <summary>
    /// The regular memberships plus the temporary memberships whose days touch the period. For readers that know a
    /// period but test no single day.
    /// </summary>
    /// <param name="from">First day of the period (inclusive)</param>
    /// <param name="until">Last day of the period (inclusive)</param>
    public static Expression<Func<GroupItem, bool>> OrTemporaryWithin(DateOnly from, DateOnly until)
    {
        var fromDay = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilDay = until.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return item => item.AnalyseToken == null
            && (!item.IsTemporary || (item.ValidFrom <= untilDay && item.ValidUntil >= fromDay));
    }

    /// <summary>The regular memberships among the group items of a client.</summary>
    public static readonly Expression<Func<Client, IEnumerable<GroupItem>>> OfClient =
        Filtered<Client>(nameof(Client.GroupItems));

    /// <summary>
    /// The group items of a group without scenario rows and temporary memberships: its regular memberships and its
    /// shift links of the real plan.
    /// </summary>
    public static readonly Expression<Func<Group, IEnumerable<GroupItem>>> ItemsOfGroup =
        Filtered<Group>(nameof(Group.GroupItems));

    /// <summary>The regular memberships among the group items of a group; its shift links are left out.</summary>
    public static readonly Expression<Func<Group, IEnumerable<GroupItem>>> MembersOfGroup =
        Filtered<Group>(nameof(Group.GroupItems), item => item.ClientId != null && item.ShiftId == null);

    private static readonly Func<GroupItem, bool> Matches = IsRegular.Compile();

    /// <summary>
    /// The clients with a regular membership that fulfils the condition, for example one in a set of groups.
    /// </summary>
    /// <param name="condition">Further condition on the regular membership</param>
    public static Expression<Func<Client, bool>> ClientHasOne(Expression<Func<GroupItem, bool>> condition)
    {
        var any = ((Func<IEnumerable<GroupItem>, Func<GroupItem, bool>, bool>)Enumerable.Any).Method;
        return Expression.Lambda<Func<Client, bool>>(Expression.Call(any, OfClient.Body, condition), OfClient.Parameters);
    }

    /// <param name="item">A loaded group item</param>
    /// <returns>True when the item is a regular membership</returns>
    public static bool Holds(GroupItem item) => Matches(item);

    private static Expression<Func<TOwner, IEnumerable<GroupItem>>> Filtered<TOwner>(
        string navigationName, Expression<Func<GroupItem, bool>>? furtherCondition = null)
    {
        var where = ((Func<IEnumerable<GroupItem>, Func<GroupItem, bool>, IEnumerable<GroupItem>>)Enumerable.Where).Method;
        var owner = Expression.Parameter(typeof(TOwner), OwnerParameterName);
        Expression items = Expression.Call(where, Expression.Property(owner, navigationName), IsRegular);
        if (furtherCondition is not null)
        {
            items = Expression.Call(where, items, furtherCondition);
        }

        return Expression.Lambda<Func<TOwner, IEnumerable<GroupItem>>>(items, owner);
    }
}
