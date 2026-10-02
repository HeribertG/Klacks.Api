// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds a location group tree from client addresses in one server-side call: region (when the
/// country ships a region map) → state/province group → city cluster, and places every client of the
/// requested types (employees, external employees and customers, or all three) into its cluster.
/// With apply=false (default) it returns a read-only preview; with apply=true it creates the missing
/// groups (reusing a group with the right name under the right parent), persists the memberships and
/// verifies the write. Cluster groups receive the mean coordinates of their addresses so the order
/// assignment's distance fallback works without geocoding.
/// </summary>
/// <param name="level">Granularity: 'cluster' (default), 'cluster_municipality', 'state', 'city' or 'state_city'.</param>
/// <param name="entityType">Client types to place: 'All' (default), 'Employee', 'ExternEmp' or 'Customer'.</param>
/// <param name="clusterSharePercent">At level 'cluster': minimum share (1-100, default 10) of a state's addresses a city needs to become its own cluster; the largest city of a state is always a cluster.</param>
/// <param name="subClusterSharePercent">At level 'cluster_municipality': minimum share (1-100, default 15) of a city cluster's addresses a place outside the centre city needs to become a municipality sub-cluster (it also needs at least three addresses).</param>
/// <param name="rootGroupName">Optional name of an existing group every top-level node attaches under; when omitted, states nest under the regions the country's region map defines.</param>
/// <param name="includeAlreadyGrouped">When false (default), clients that already hold an active group membership are left untouched.</param>
/// <param name="validFrom">Start date of the new memberships (format YYYY-MM-DD, or 'today'); defaults to today when omitted.</param>
/// <param name="apply">When false (default) only previews the plan; when true creates the groups and persists the memberships.</param>

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Grouping;
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
        "Invalid level '{0}'. Allowed: cluster, cluster_municipality, state, city, state_city.";

    private const string LevelCluster = "cluster";
    private const string LevelState = "state";
    private const string LevelCity = "city";
    private const string LevelStateCity = "state_city";
    private const string LevelClusterMunicipality = "cluster_municipality";

    private const string MunicipalityTreeDescription =
        "Tree: region → state → city cluster (named after its centre city) → municipality sub-cluster. " +
        "Clients of the centre city stay directly in the city cluster group.";

    private const int MaxPreviewPlaceAttachments = 20;

    private const string InvalidEntityTypeError =
        "Invalid entityType '{0}'. Allowed: All, Employee, ExternEmp, Customer.";

    private const string InvalidClusterShareError =
        "clusterSharePercent must be between {0} and {1}; got {2}.";

    private const string InvalidSubClusterShareError =
        "subClusterSharePercent must be between {0} and {1}; got {2}.";

    private const string EntityTypeAll = "All";
    private const int MinClusterSharePercent = 1;
    private const int MaxClusterSharePercent = 100;

    private static readonly IReadOnlyList<EntityTypeEnum> AllEntityTypes =
        [EntityTypeEnum.Employee, EntityTypeEnum.ExternEmp, EntityTypeEnum.Customer];

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
        var levelStr = GetParameter<string>(parameters, "level") ?? LevelCluster;
        if (!TryParseLevel(levelStr, out var level))
        {
            return SkillResult.Error(string.Format(InvalidLevelError, levelStr));
        }

        var entityTypeStr = GetParameter<string>(parameters, "entityType");
        if (!TryParseEntityTypes(entityTypeStr, out var entityTypes))
        {
            return SkillResult.Error(string.Format(InvalidEntityTypeError, entityTypeStr));
        }

        if (!TryParseClusterSharePercent(GetParameter<int?>(parameters, "clusterSharePercent"), out var clusterSharePercent))
        {
            return SkillResult.Error(string.Format(
                InvalidClusterShareError, MinClusterSharePercent, MaxClusterSharePercent, clusterSharePercent));
        }

        if (!TryParseSharePercent(
                GetParameter<int?>(parameters, "subClusterSharePercent"),
                GroupPartitionContext.DefaultSubClusterSharePercent,
                out var subClusterSharePercent))
        {
            return SkillResult.Error(string.Format(
                InvalidSubClusterShareError, MinClusterSharePercent, MaxClusterSharePercent, subClusterSharePercent));
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

        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            validFromStr, today, context.UserLanguage);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validFrom", validFromStr!));
        }

        PartitionClientsByAddressResult result;
        try
        {
            result = await _mediator.Send(
                new PartitionClientsByAddressCommand(
                    level, entityTypes, rootGroupId, rootGroupName, includeAlreadyGrouped, validFrom, apply,
                    context.UserName, clusterSharePercent, subClusterSharePercent),
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
                $"None of the {result.TotalClients} {result.EntityType} client(s) could be placed at level " +
                $"'{result.Level}'. {BuildDiagnostics(result)} Nothing was changed.");
        }

        var visibilityAdvisory = FirstGroupVisibilityAdvisory.For(result.UsersKeepingFullVisibilityCount);

        var isMunicipalityLevel = result.Level == nameof(GroupPartitionLevelEnum.ClusterMunicipality);
        var groupNames = string.Join(", ",
            result.Groups.Take(MaxPreviewGroupNames)
                .Select(g => $"{g.Name} ({g.ClientCount}{(g.Existed ? ", existing" : ", new")}" +
                    $"{(isMunicipalityLevel && g.ParentName != null ? $", in {g.ParentName}" : string.Empty)})"));
        var moreGroups = result.Groups.Count > MaxPreviewGroupNames
            ? $" (+{result.Groups.Count - MaxPreviewGroupNames} more)"
            : string.Empty;
        var newCount = result.Groups.Count(g => !g.Existed);

        return SkillResult.SuccessResult(
            result,
            $"Preview: {result.Groups.Count} group(s) planned at level '{result.Level}' ({newCount} new, " +
            $"{result.Groups.Count - newCount} reused): {groupNames}{moreGroups}. " +
            (isMunicipalityLevel ? $"{MunicipalityTreeDescription} {BuildPlaceAttachments(result)}" : string.Empty) +
            $"{BuildDiagnostics(result)} {visibilityAdvisory} Nothing was changed yet. " +
            "Ask the user to confirm, then call again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(PartitionClientsByAddressResult result)
    {
        var alreadyNote = result.AlreadyMemberCount > 0
            ? $" ({result.AlreadyMemberCount} were already members)"
            : string.Empty;
        var newCount = result.Groups.Count(g => !g.Existed);
        var reusedCount = result.Groups.Count - newCount;
        var visibilityAdvisory = FirstGroupVisibilityAdvisory.For(result.UsersKeepingFullVisibilityCount);

        return SkillResult.SuccessResult(
            result,
            $"Partitioned {result.TotalClients} {result.EntityType} client(s) at level '{result.Level}' into " +
            $"{result.Groups.Count} group(s) ({newCount} new, {reusedCount} reused), added {result.AssignedCount} " +
            $"membership(s) and confirmed {result.VerifiedCount} in the database (verified){alreadyNote}. " +
            $"{BuildDiagnostics(result)} {visibilityAdvisory}");
    }

    private static string BuildPlaceAttachments(PartitionClientsByAddressResult result)
    {
        var attachments = result.PlaceAttachments ?? [];
        if (attachments.Count == 0)
        {
            return "No smaller place had to be attached. ";
        }

        var listed = string.Join("; ", attachments.Take(MaxPreviewPlaceAttachments).Select(a => a.JoinedSubCluster
            ? $"{a.Place} ({a.ClientCount}) joins the nearer sub-cluster {a.TargetGroupName}"
            : $"{a.Place} ({a.ClientCount}) stays directly in {a.TargetGroupName}"));
        var more = attachments.Count > MaxPreviewPlaceAttachments
            ? $" (+{attachments.Count - MaxPreviewPlaceAttachments} more)"
            : string.Empty;

        return $"Smaller places: {listed}{more}. ";
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

    private static bool TryParseEntityTypes(string? value, out IReadOnlyList<EntityTypeEnum> entityTypes)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), EntityTypeAll, StringComparison.OrdinalIgnoreCase))
        {
            entityTypes = AllEntityTypes;
            return true;
        }

        if (Enum.TryParse<EntityTypeEnum>(value.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            entityTypes = [parsed];
            return true;
        }

        entityTypes = AllEntityTypes;
        return false;
    }

    private static bool TryParseClusterSharePercent(int? value, out int share) =>
        TryParseSharePercent(value, GroupPartitionContext.DefaultClusterSharePercent, out share);

    private static bool TryParseSharePercent(int? value, int defaultShare, out int share)
    {
        share = value ?? defaultShare;
        return share >= MinClusterSharePercent && share <= MaxClusterSharePercent;
    }

    private static bool TryParseLevel(string value, out GroupPartitionLevelEnum level)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case LevelCluster:
                level = GroupPartitionLevelEnum.Cluster;
                return true;
            case LevelState:
                level = GroupPartitionLevelEnum.State;
                return true;
            case LevelCity:
                level = GroupPartitionLevelEnum.City;
                return true;
            case LevelStateCity:
                level = GroupPartitionLevelEnum.StateCity;
                return true;
            case LevelClusterMunicipality:
                level = GroupPartitionLevelEnum.ClusterMunicipality;
                return true;
            default:
                level = GroupPartitionLevelEnum.Cluster;
                return false;
        }
    }
}
