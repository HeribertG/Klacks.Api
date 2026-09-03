// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Splits every client of a given entity type into a region/canton/city group hierarchy built from
/// their current address, creating the missing groups and the memberships in one server-side call —
/// the bulk counterpart to filling one group at a time. With apply=false (default) it returns a
/// read-only preview of the planned groups and placements; with apply=true it persists them and
/// verifies the write. Reusable in an already-partly-grouped install: an existing group with the right
/// name under the right parent is reused instead of duplicated, and clients that already hold a group
/// membership are skipped unless includeAlreadyGrouped is set.
/// </summary>
/// <param name="level">Granularity: 'canton', 'city' or 'canton_city' (default); canton_city nests a city group under its canton group.</param>
/// <param name="entityType">Client type to partition: 'Employee' (default) or 'ExternEmp'. 'Customer' is rejected — customers are placed with the customer-grouping tools instead.</param>
/// <param name="rootGroupName">Optional name of an existing group every top-level node (canton, or city at city level) attaches under; when omitted, cantons are nested under the same region roots the demo seed uses.</param>
/// <param name="includeAlreadyGrouped">When false (default), clients that already hold an active group membership are left untouched.</param>
/// <param name="validFrom">Start date of the new memberships (format YYYY-MM-DD, or 'today'); defaults to today when omitted.</param>
/// <param name="apply">When false (default) only previews the plan; when true creates the groups and persists the memberships.</param>

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("partition_clients_by_address")]
public class PartitionClientsByAddressSkill : BaseSkillImplementation
{
    private const int MaxPreviewGroupNames = 20;
    private const int MaxUnassignablePreviewNames = 10;

    private const string RestrictedScopeError =
        "This skill partitions the whole client population and creates groups at the top of the tree; " +
        "it is only available to users with unrestricted group scope. Your scope is limited to: {0}. " +
        "Ask an administrator to run it, or build the groups inside your scope one at a time instead.";

    private const string InvalidLevelError =
        "Invalid level '{0}'. Allowed: canton, city, canton_city.";

    private const string CustomerEntityTypeError =
        "entityType 'Customer' is not supported by this skill: the ERP import creates customers as " +
        "clients too, and this skill is meant for the staff address book. Use the customer-grouping " +
        "tools for customers instead.";

    private const string InvalidEntityTypeError =
        "Invalid entityType '{0}'. Allowed: Employee, ExternEmp.";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public PartitionClientsByAddressSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator,
        ICompanyClock companyClock)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var levelStr = GetParameter<string>(parameters, "level") ?? "canton_city";
        if (!TryParseLevel(levelStr, out var level))
        {
            return SkillResult.Error(string.Format(InvalidLevelError, levelStr));
        }

        var entityTypeStr = GetParameter<string>(parameters, "entityType");
        if (string.Equals(entityTypeStr, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(CustomerEntityTypeError);
        }

        EntityTypeEnum entityType;
        if (string.IsNullOrWhiteSpace(entityTypeStr))
        {
            entityType = EntityTypeEnum.Employee;
        }
        else if (string.Equals(entityTypeStr, "Employee", StringComparison.OrdinalIgnoreCase))
        {
            entityType = EntityTypeEnum.Employee;
        }
        else if (string.Equals(entityTypeStr, "ExternEmp", StringComparison.OrdinalIgnoreCase))
        {
            entityType = EntityTypeEnum.ExternEmp;
        }
        else
        {
            return SkillResult.Error(string.Format(InvalidEntityTypeError, entityTypeStr));
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsUnrestricted)
        {
            return SkillResult.Error(string.Format(RestrictedScopeError, string.Join(", ", scope.VisibleRootNames)));
        }

        var rootGroupName = GetParameter<string>(parameters, "rootGroupName");
        Guid? rootGroupId = null;
        if (!string.IsNullOrWhiteSpace(rootGroupName))
        {
            var groups = await _groupRepository.List();
            var (rootGroup, rootGroupError) = GroupResolver.Resolve(groups, rootGroupName);
            if (rootGroup == null)
            {
                return SkillResult.Error(rootGroupError!);
            }

            rootGroupId = rootGroup.Id;
            rootGroupName = rootGroup.Name;
        }

        var includeAlreadyGrouped = GetParameter<bool?>(parameters, "includeAlreadyGrouped") ?? false;
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"), today);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        PartitionClientsByAddressResult result;
        try
        {
            result = await _mediator.Send(
                new PartitionClientsByAddressCommand(
                    level, entityType, rootGroupId, rootGroupName, includeAlreadyGrouped, validFrom, apply, context.UserName),
                cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return apply ? BuildAppliedResult(result) : BuildPreviewResult(result);
    }

    private static SkillResult BuildPreviewResult(PartitionClientsByAddressResult result)
    {
        if (result.Groups.Count == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"None of the {result.TotalClients} {result.EntityType}(s) could be placed at level " +
                $"'{result.Level}'. {BuildDiagnostics(result)} Nothing was changed.");
        }

        var groupNames = string.Join(", ",
            result.Groups.Take(MaxPreviewGroupNames)
                .Select(g => $"{g.Name} ({g.ClientCount}{(g.Existed ? ", existing" : ", new")})"));
        var moreGroups = result.Groups.Count > MaxPreviewGroupNames
            ? $" (+{result.Groups.Count - MaxPreviewGroupNames} more)"
            : string.Empty;
        var newCount = result.Groups.Count(g => !g.Existed);

        return SkillResult.SuccessResult(
            result,
            $"Preview: {result.Groups.Count} group(s) planned at level '{result.Level}' ({newCount} new, " +
            $"{result.Groups.Count - newCount} reused): {groupNames}{moreGroups}. " +
            $"{BuildDiagnostics(result)} Nothing was changed yet. " +
            "Ask the user to confirm, then call again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(PartitionClientsByAddressResult result)
    {
        var alreadyNote = result.AlreadyMemberCount > 0
            ? $" ({result.AlreadyMemberCount} were already members)"
            : string.Empty;

        return SkillResult.SuccessResult(
            result,
            $"Partitioned {result.TotalClients} {result.EntityType}(s) at level '{result.Level}' into " +
            $"{result.Groups.Count} group(s), added {result.AssignedCount} membership(s) and confirmed " +
            $"{result.VerifiedCount} in the database (verified){alreadyNote}. {BuildDiagnostics(result)}");
    }

    private static string BuildDiagnostics(PartitionClientsByAddressResult result)
    {
        var parts = new List<string>();

        if (result.SkippedAlreadyGroupedCount > 0)
        {
            parts.Add($"{result.SkippedAlreadyGroupedCount} already had a group and were skipped");
        }

        if (result.UnassignableCount > 0)
        {
            var sample = string.Join(", ",
                result.UnassignableSample.Take(MaxUnassignablePreviewNames)
                    .Select(u => $"{u.ClientName} ({u.Reason})"));
            var more = result.UnassignableCount > MaxUnassignablePreviewNames
                ? $" and {result.UnassignableCount - MaxUnassignablePreviewNames} more"
                : string.Empty;
            parts.Add($"{result.UnassignableCount} could not be placed: {sample}{more}");
        }

        if (result.Warnings.Count > 0)
        {
            parts.Add("warnings: " + string.Join(" ", result.Warnings));
        }

        return parts.Count > 0 ? string.Join("; ", parts) + "." : string.Empty;
    }

    private static bool TryParseLevel(string value, out GroupPartitionLevelEnum level)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "canton":
                level = GroupPartitionLevelEnum.Canton;
                return true;
            case "city":
                level = GroupPartitionLevelEnum.City;
                return true;
            case "canton_city":
                level = GroupPartitionLevelEnum.CantonCity;
                return true;
            default:
                level = GroupPartitionLevelEnum.CantonCity;
                return false;
        }
    }
}
