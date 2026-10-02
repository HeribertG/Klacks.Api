// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates one group per qualification in one server-side call and adds every client holding that qualification
/// today, so a client with several qualifications joins several groups (existing memberships stay). Group names
/// are the qualification names in the installation language. Without rootGroupName the groups go under a
/// top-level group named "Qualifications" in the installation language (reused when it exists); with
/// rootGroupName they go under that group and only its members (including its subgroups) are considered.
/// With apply=false (default) it returns a read-only preview; with apply=true it creates the missing groups
/// (reusing a group with the same name under the same parent), persists the memberships and verifies the write.
/// </summary>
/// <param name="entityType">Client types: 'Employee' (default), 'ExternEmp', 'Customer' or 'All'.</param>
/// <param name="rootGroupName">Optional existing group the qualification groups go under; limits the run to its members.</param>
/// <param name="minMembers">Minimum number of holders a qualification needs to get a group; default 2, values below 1 count as 1.</param>
/// <param name="includeAlreadyGrouped">When true (default) clients with other memberships are included too; when false they are skipped (memberships inside rootGroupName's subtree do not count).</param>
/// <param name="validFrom">Start date of the new memberships (YYYY-MM-DD or 'today'); defaults to today.</param>
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

[SkillImplementation("partition_clients_by_qualification")]
public class PartitionClientsByQualificationSkill : BaseSkillImplementation
{
    private const int MaxPreviewGroups = 30;
    private const int MaxPreviewSkipped = 15;

    private const string RestrictedScopeError =
        "This skill groups the whole client population and may create a group at the top of the tree; " +
        "it is only available to users with unrestricted group scope. Your scope is limited to: {0}. " +
        "Ask an administrator to run it.";

    private const string InvalidEntityTypeError =
        "Invalid entityType '{0}'. Allowed: Employee, ExternEmp, Customer, All.";

    private static readonly IReadOnlyList<EntityTypeEnum> AllEntityTypes =
        [EntityTypeEnum.Employee, EntityTypeEnum.ExternEmp, EntityTypeEnum.Customer];

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public PartitionClientsByQualificationSkill(
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
        var entityTypeStr = GetParameter<string>(parameters, "entityType");
        if (!TryParseEntityTypes(entityTypeStr, out var entityTypes))
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
        else
        {
            rootGroupName = null;
        }

        var minMembers = Math.Max(
            PartitionClientsByQualificationCommand.MinimumMinMembers,
            GetParameter<int?>(parameters, "minMembers") ?? PartitionClientsByQualificationCommand.DefaultMinMembers);
        var includeAlreadyGrouped = GetParameter<bool?>(parameters, "includeAlreadyGrouped") ?? true;
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(validFromStr, today, context.UserLanguage);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validFrom", validFromStr!));
        }

        PartitionClientsByQualificationResult result;
        try
        {
            result = await _mediator.Send(
                new PartitionClientsByQualificationCommand(
                    entityTypes, rootGroupId, rootGroupName, minMembers, includeAlreadyGrouped, validFrom, apply,
                    context.UserName),
                cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return apply ? BuildAppliedResult(result) : BuildPreviewResult(result);
    }

    private static SkillResult BuildPreviewResult(PartitionClientsByQualificationResult result)
    {
        if (result.Groups.Count == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"No qualification is held today by at least {result.MinMembers} of the {result.ConsideredClients} " +
                $"considered {result.EntityType} client(s){ScopeNote(result)}. {BuildDiagnostics(result)} Nothing was changed.");
        }

        var groups = string.Join(", ", result.Groups.Take(MaxPreviewGroups).Select(g => g.Existed
            ? $"{g.Name} (already exists, {g.NewMemberCount} new of {g.MemberCount} members)"
            : $"{g.Name} (new, {g.MemberCount} members)"));
        var moreGroups = result.Groups.Count > MaxPreviewGroups
            ? $" (+{result.Groups.Count - MaxPreviewGroups} more)"
            : string.Empty;
        var parent = result.ParentExisted
            ? $"under the existing group '{result.ParentGroupName}'"
            : $"under a new top-level group '{result.ParentGroupName}'";
        var visibilityAdvisory = FirstGroupVisibilityAdvisory.For(result.UsersKeepingFullVisibilityCount);

        return SkillResult.SuccessResult(
            result,
            $"Preview: {result.Groups.Count} qualification group(s) {parent}{ScopeNote(result)}: {groups}{moreGroups}. " +
            $"{result.AssignedCount} membership(s) would be added; clients holding several qualifications join " +
            $"several groups and keep their existing memberships. {BuildDiagnostics(result)} {visibilityAdvisory} " +
            "Nothing was changed yet. Ask the user to confirm, then call again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(PartitionClientsByQualificationResult result)
    {
        var newCount = result.Groups.Count(g => !g.Existed);
        var alreadyNote = result.AlreadyMemberCount > 0
            ? $" ({result.AlreadyMemberCount} were already members)"
            : string.Empty;
        var visibilityAdvisory = FirstGroupVisibilityAdvisory.For(result.UsersKeepingFullVisibilityCount);

        return SkillResult.SuccessResult(
            result,
            $"Grouped {result.ConsideredClients} {result.EntityType} client(s) by qualification under " +
            $"'{result.ParentGroupName}': {result.Groups.Count} group(s) ({newCount} new, {result.Groups.Count - newCount} " +
            $"reused), added {result.AssignedCount} membership(s) and confirmed {result.VerifiedCount} in the database " +
            $"(verified){alreadyNote}. {BuildDiagnostics(result)} {visibilityAdvisory}");
    }

    private static string ScopeNote(PartitionClientsByQualificationResult result) =>
        result.IsScoped ? $" (only members of '{result.ParentGroupName}' and its subgroups)" : string.Empty;

    private static string BuildDiagnostics(PartitionClientsByQualificationResult result)
    {
        var parts = new List<string>();

        if (result.SkippedQualifications.Count > 0)
        {
            var listed = string.Join(", ", result.SkippedQualifications.Take(MaxPreviewSkipped)
                .Select(s => $"{s.Name} ({s.MemberCount})"));
            var more = result.SkippedQualifications.Count > MaxPreviewSkipped
                ? $" and {result.SkippedQualifications.Count - MaxPreviewSkipped} more"
                : string.Empty;
            parts.Add($"skipped, fewer than {result.MinMembers} holders: {listed}{more}");
        }

        if (result.SkippedAlreadyGroupedCount > 0)
        {
            parts.Add($"{result.SkippedAlreadyGroupedCount} already had a group and were skipped");
        }

        if (result.ClientsWithoutQualificationCount > 0)
        {
            parts.Add($"{result.ClientsWithoutQualificationCount} hold no qualification valid today");
        }

        if (result.RestrictedUsersSeeingRootCount > 0 && result.ClientsNewlyVisibleToThemCount > 0)
        {
            parts.Add(
                $"visibility: {result.RestrictedUsersSeeingRootCount} restricted user(s) who see '{result.ParentGroupName}' " +
                $"will additionally see {result.ClientsNewlyVisibleToThemCount} people - tell the user before applying");
        }

        if (result.Warnings.Count > 0)
        {
            parts.Add("warnings: " + string.Join(" ", result.Warnings));
        }

        return parts.Count > 0 ? string.Join("; ", parts) + "." : string.Empty;
    }

    private static bool TryParseEntityTypes(string? value, out IReadOnlyList<EntityTypeEnum> entityTypes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            entityTypes = [EntityTypeEnum.Employee];
            return true;
        }

        if (string.Equals(value.Trim(), PartitionClientsByQualificationCommand.AllEntityTypesLabel, StringComparison.OrdinalIgnoreCase))
        {
            entityTypes = AllEntityTypes;
            return true;
        }

        if (Enum.TryParse<EntityTypeEnum>(value.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            entityTypes = [parsed];
            return true;
        }

        entityTypes = [EntityTypeEnum.Employee];
        return false;
    }
}
