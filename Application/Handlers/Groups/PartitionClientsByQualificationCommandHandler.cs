// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="PartitionClientsByQualificationCommand"/>. Delegates the plan to the pure
/// <see cref="QualificationGroupPlanner"/>; with Apply=false it only returns the plan, with Apply=true it creates
/// the missing groups top-down through <see cref="IGroupRepository.Add"/> inside one transaction (the qualifications
/// root group first; every Add saves its nested-set row within that transaction before the next child is inserted),
/// stages the new memberships, saves them with a single CompleteAsync at the end and re-reads them to confirm the
/// write; a mismatch rolls the whole transaction back. When an existing qualifications root is reused (no scope
/// group), the result reports how many non-admin users already see that root and how many clients they would
/// additionally see, because group visibility is inherited through the root. Group and
/// qualification names use the installation language (DEFAULT_LANGUAGE, then its base language, then the core
/// languages), never the chat language. Without a scope group the qualification groups go under the top-level
/// group named by <see cref="QualificationGroupRootNames"/>, which is reused when it exists once and refused when
/// it exists several times. Only unrestricted callers may run it; a restricted caller is refused before anything
/// is read.
/// </summary>
/// <param name="clientRepository">Loads clients of every requested type with qualifications and memberships.</param>
/// <param name="qualificationRepository">Loads the qualification master records for the group names.</param>
/// <param name="groupRepository">Provides the existing groups and creates the missing ones (nested-set aware).</param>
/// <param name="groupItemRepository">Adds the new memberships and counts them for verification.</param>
/// <param name="unitOfWork">Commits and verifies the new memberships in a single transaction.</param>
/// <param name="companyClock">Supplies the company-local date for validity and the default ValidFrom.</param>
/// <param name="languageResolver">Supplies the installation language for the group names.</param>
/// <param name="groupVisibilityRepository">Counts the non-admin users with explicit visibility on a reused qualifications root.</param>
/// <param name="visibilityPreservation">Counts the users that keep access to everything when this run introduces the first group.</param>
/// <param name="groupVisibilityGuard">Tells whether the caller is free of group-visibility restrictions.</param>
public sealed class PartitionClientsByQualificationCommandHandler
    : IRequestHandler<PartitionClientsByQualificationCommand, PartitionClientsByQualificationResult>
{
    private const string SkillName = "partition_clients_by_qualification";
    private const char RegionSeparator = '-';
    private const string RestrictedCallerMessage = "Only unrestricted users may partition clients into groups";

    private const string AmbiguousRootMessage =
        "There are {0} top-level groups named '{1}', so it is unclear which one should hold the qualification groups. " +
        "Ask the user to rename or delete the duplicates first, then run this again. Candidates: {2}.";

    private readonly IClientRepository _clientRepository;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;
    private readonly IInstallationLanguageResolver _languageResolver;
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IGroupVisibilityPreservationService _visibilityPreservation;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public PartitionClientsByQualificationCommandHandler(
        IClientRepository clientRepository,
        IQualificationRepository qualificationRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock,
        IInstallationLanguageResolver languageResolver,
        IGroupVisibilityRepository groupVisibilityRepository,
        IGroupVisibilityPreservationService visibilityPreservation,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _clientRepository = clientRepository;
        _qualificationRepository = qualificationRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
        _languageResolver = languageResolver;
        _groupVisibilityRepository = groupVisibilityRepository;
        _visibilityPreservation = visibilityPreservation;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<PartitionClientsByQualificationResult> Handle(
        PartitionClientsByQualificationCommand request, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            throw new ForbiddenException(RestrictedCallerMessage);
        }

        var todayDate = await _companyClock.GetTodayAsync(cancellationToken);
        var today = DateOnly.FromDateTime(todayDate);
        var language = await _languageResolver.ResolveAsync(cancellationToken);

        var existingGroups = (await _groupRepository.List()).Where(g => !g.IsDeleted).ToList();
        var parent = ResolveParent(request, existingGroups, language);
        var scopeGroupIds = request.ScopeGroupId is { } scopeId ? CollectSubtree(scopeId, existingGroups) : null;
        var ownSubtreeGroupIds = parent.Id is { } parentId ? CollectSubtree(parentId, existingGroups) : null;

        var clients = new List<Client>();
        foreach (var entityType in request.EntityTypes.Distinct())
        {
            clients.AddRange(await _clientRepository.GetByTypeWithQualificationsAndGroupItemsAsync(entityType, cancellationToken));
        }

        var qualifications = await _qualificationRepository.GetAllAsync(cancellationToken);
        var usersKeepingFullVisibility = await _visibilityPreservation.CountUsersKeepingFullVisibilityAsync(cancellationToken);

        var context = new QualificationGroupPlanContext(
            NameLanguages(language), today, parent.Id, scopeGroupIds, request.EffectiveMinMembers, request.IncludeAlreadyGrouped,
            ownSubtreeGroupIds);
        var plan = QualificationGroupPlanner.Plan(clients, qualifications, existingGroups, context);
        var widening = await ResolveVisibilityWideningAsync(request, parent, ownSubtreeGroupIds, clients, plan, today, cancellationToken);

        var plannedNew = plan.Groups.Sum(g => g.NewMemberClientIds.Count);
        var plannedAlready = plan.Groups.Sum(g => g.MemberClientIds.Count - g.NewMemberClientIds.Count);

        if (!request.Apply)
        {
            return BuildResult(
                request, plan, parent, applied: false, groupIds: null, assignedCount: plannedNew, verifiedCount: 0,
                alreadyMemberCount: plannedAlready, usersKeepingFullVisibility, widening);
        }

        var validFrom = request.ValidFrom ?? todayDate;
        var now = DateTime.UtcNow;

        var outcome = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var parentId = parent.Id;
            if (parentId is null)
            {
                var root = NewGroup(parent.Name, null, validFrom, now, request.UserName);
                await _groupRepository.Add(root);
                parentId = root.Id;
            }

            var groupIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var planned in plan.Groups)
            {
                if (planned.Existed)
                {
                    groupIds[planned.Name] = planned.ExistingGroupId!.Value;
                    continue;
                }

                var group = NewGroup(planned.Name, parentId, validFrom, now, request.UserName);
                await _groupRepository.Add(group);
                groupIds[planned.Name] = group.Id;
            }

            var newItems = plan.Groups
                .SelectMany(planned => planned.NewMemberClientIds.Select(clientId => new GroupItem
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    GroupId = groupIds[planned.Name],
                    ValidFrom = validFrom,
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                }))
                .ToList();

            foreach (var item in newItems)
            {
                await _groupItemRepository.Add(item);
            }

            await _unitOfWork.CompleteAsync();

            var confirmed = newItems.Count == 0
                ? 0
                : await _groupItemRepository.CountExistingByIds(newItems.Select(i => i.Id).ToList(), cancellationToken);
            if (confirmed != newItems.Count)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Database verification failed: expected {newItems.Count} new memberships but only " +
                    $"{confirmed} were confirmed — the changes were rolled back.");
            }

            return new QualificationApplyOutcome(confirmed, parentId.Value, groupIds);
        });

        return BuildResult(
            request, plan, parent with { Id = outcome.ParentGroupId }, applied: true, outcome.GroupIds,
            assignedCount: plannedNew, verifiedCount: outcome.VerifiedCount, alreadyMemberCount: plannedAlready,
            usersKeepingFullVisibility, widening);
    }

    private async Task<VisibilityWidening> ResolveVisibilityWideningAsync(
        PartitionClientsByQualificationCommand request,
        ParentResolution parent,
        IReadOnlySet<Guid>? ownSubtreeGroupIds,
        IReadOnlyList<Client> clients,
        QualificationGroupPlan plan,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (request.ScopeGroupId.HasValue || parent.Id is not { } rootId || ownSubtreeGroupIds is null)
        {
            return VisibilityWidening.None;
        }

        var newMemberIds = plan.Groups.SelectMany(g => g.NewMemberClientIds).ToHashSet();
        var newlyVisible = clients
            .Where(c => newMemberIds.Contains(c.Id))
            .Count(c => !c.GroupItems.Any(gi =>
                ownSubtreeGroupIds.Contains(gi.GroupId) && QualificationGroupPlanner.IsCurrentMembership(gi, today)));
        if (newlyVisible == 0)
        {
            return VisibilityWidening.None;
        }

        var users = await _groupVisibilityRepository.CountNonAdminUsersSeeingGroupAsync(rootId, cancellationToken);
        return users == 0 ? VisibilityWidening.None : new VisibilityWidening(users, newlyVisible);
    }

    private static ParentResolution ResolveParent(
        PartitionClientsByQualificationCommand request, IReadOnlyList<Group> existingGroups, string language)
    {
        if (request.ScopeGroupId is { } scopeId)
        {
            return new ParentResolution(scopeId, request.ScopeGroupName ?? string.Empty, Existed: true);
        }

        var rootName = QualificationGroupRootNames.Resolve(language);
        var candidates = existingGroups
            .Where(g => g.Parent == null && string.Equals(g.Name?.Trim(), rootName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Id)
            .ToList();

        if (candidates.Count > 1)
        {
            throw new InvalidRequestException(string.Format(
                AmbiguousRootMessage, candidates.Count, rootName, string.Join(", ", candidates.Select(g => g.Id))));
        }

        var existing = candidates.FirstOrDefault();
        return new ParentResolution(existing?.Id, existing?.Name ?? rootName, existing != null);
    }

    private static HashSet<Guid> CollectSubtree(Guid rootId, IReadOnlyList<Group> groups)
    {
        var childrenByParent = groups
            .Where(g => g.Parent.HasValue)
            .ToLookup(g => g.Parent!.Value, g => g.Id);

        var subtree = new HashSet<Guid> { rootId };
        var pending = new Queue<Guid>();
        pending.Enqueue(rootId);
        while (pending.Count > 0)
        {
            foreach (var childId in childrenByParent[pending.Dequeue()])
            {
                if (subtree.Add(childId))
                {
                    pending.Enqueue(childId);
                }
            }
        }

        return subtree;
    }

    private static IReadOnlyList<string> NameLanguages(string language)
    {
        var languages = new List<string>();
        if (!string.IsNullOrWhiteSpace(language))
        {
            var tag = language.Trim();
            languages.Add(tag);
            var separatorIndex = tag.IndexOf(RegionSeparator);
            if (separatorIndex > 0)
            {
                languages.Add(tag[..separatorIndex]);
            }
        }

        languages.AddRange(MultiLanguage.CoreLanguages);
        return languages.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static Group NewGroup(string name, Guid? parentId, DateTime validFrom, DateTime now, string userName) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = string.Empty,
        Parent = parentId,
        ValidFrom = validFrom,
        PaymentInterval = PaymentInterval.Monthly,
        CreateTime = now,
        CurrentUserCreated = userName
    };

    private static PartitionClientsByQualificationResult BuildResult(
        PartitionClientsByQualificationCommand request,
        QualificationGroupPlan plan,
        ParentResolution parent,
        bool applied,
        IReadOnlyDictionary<string, Guid>? groupIds,
        int assignedCount,
        int verifiedCount,
        int alreadyMemberCount,
        int usersKeepingFullVisibilityCount,
        VisibilityWidening widening) =>
        new(
            Applied: applied,
            EntityType: request.EntityTypes.Distinct().Count() == 1
                ? request.EntityTypes.First().ToString()
                : PartitionClientsByQualificationCommand.AllEntityTypesLabel,
            ParentGroupName: parent.Name,
            ParentExisted: parent.Existed,
            ParentGroupId: parent.Id,
            IsScoped: request.ScopeGroupId.HasValue,
            MinMembers: request.EffectiveMinMembers,
            TotalClients: plan.TotalClients,
            ConsideredClients: plan.ConsideredClients,
            SkippedAlreadyGroupedCount: plan.SkippedAlreadyGroupedCount,
            ClientsWithoutQualificationCount: plan.ClientsWithoutQualificationCount,
            AssignedCount: assignedCount,
            VerifiedCount: verifiedCount,
            AlreadyMemberCount: alreadyMemberCount,
            Groups: plan.Groups
                .Select(g => new QualificationGroupSummary(
                    g.Name,
                    g.Existed,
                    groupIds != null && groupIds.TryGetValue(g.Name, out var id) ? id : g.ExistingGroupId,
                    g.MemberClientIds.Count,
                    g.NewMemberClientIds.Count))
                .ToList(),
            SkippedQualifications: plan.Skipped,
            Warnings: plan.Warnings,
            UsersKeepingFullVisibilityCount: usersKeepingFullVisibilityCount,
            RestrictedUsersSeeingRootCount: widening.RestrictedUsers,
            ClientsNewlyVisibleToThemCount: widening.NewlyVisibleClients);

    private sealed record ParentResolution(Guid? Id, string Name, bool Existed);

    private sealed record VisibilityWidening(int RestrictedUsers, int NewlyVisibleClients)
    {
        public static VisibilityWidening None { get; } = new(0, 0);
    }
}
