// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="AddSelectedClientsToGroupCommand"/>. Loads the selected clients by id, skips
/// the ones already in the target group, and either previews the eligible clients (Apply=false) or adds
/// them in a single transaction that re-reads the new memberships and rolls back on a verification
/// mismatch. Mirrors the direct group_item persistence used by the criteria-fill handler, so re-running
/// it never creates duplicates. A group-restricted caller may only target a visible group (a hidden one is
/// answered like a missing one), and selected clients the caller cannot see are counted as not found.
/// </summary>
/// <param name="clientRepository">Loads the selected clients by id.</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the target group.</param>
/// <param name="clientVisibilityGuard">Drops the selected clients the caller cannot see.</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits the new memberships in a single verified transaction.</param>
/// <param name="companyClock">Supplies the company-local date used when no explicit ValidFrom is given.</param>
public sealed class AddSelectedClientsToGroupCommandHandler
    : IRequestHandler<AddSelectedClientsToGroupCommand, AddSelectedClientsToGroupResult>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public AddSelectedClientsToGroupCommandHandler(
        IClientRepository clientRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard)
    {
        _clientRepository = clientRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<AddSelectedClientsToGroupResult> Handle(
        AddSelectedClientsToGroupCommand request, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.GroupId, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, request.GroupId));
        }

        var requestedCount = request.SelectedClientIds.Count;
        var loadedClients = await _clientRepository.GetByIdsAsync(request.SelectedClientIds, cancellationToken);
        var clients = await _clientVisibilityGuard.FilterVisibleAsync(loadedClients, c => c.Id, cancellationToken);

        var eligible = new List<Client>();
        var alreadyMember = 0;
        foreach (var client in clients)
        {
            var existing = await _groupItemRepository.GetByClientAndGroup(client.Id, request.GroupId);
            if (existing != null && !existing.IsDeleted)
            {
                alreadyMember++;
                continue;
            }

            eligible.Add(client);
        }

        var eligibleItems = eligible.Select(ToSearchItem).ToList();
        var notFound = requestedCount - clients.Count;

        if (!request.Apply)
        {
            return new AddSelectedClientsToGroupResult(
                Applied: false,
                GroupName: request.GroupName,
                RequestedCount: requestedCount,
                FoundCount: clients.Count,
                NotFoundCount: notFound,
                EligibleCount: eligible.Count,
                AddedCount: 0,
                VerifiedCount: 0,
                AlreadyMemberCount: alreadyMember,
                Clients: eligibleItems);
        }

        var now = DateTime.UtcNow;
        var validFrom = request.ValidFrom ?? await _companyClock.GetTodayAsync(cancellationToken);
        var newItems = eligible.Select(c => new GroupItem
        {
            Id = Guid.NewGuid(),
            ClientId = c.Id,
            GroupId = request.GroupId,
            ValidFrom = validFrom,
            CreateTime = now,
            CurrentUserCreated = request.UserName
        }).ToList();

        var verified = 0;
        if (newItems.Count > 0)
        {
            verified = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var item in newItems)
                {
                    await _groupItemRepository.Add(item);
                }

                await _unitOfWork.CompleteAsync();

                var confirmed = await _groupItemRepository.CountExistingByIds(
                    newItems.Select(i => i.Id).ToList(), cancellationToken);
                if (confirmed != newItems.Count)
                {
                    throw new SkillVerificationException(
                        "add_selected_clients_to_group",
                        $"Database verification failed: expected {newItems.Count} new memberships in group " +
                        $"'{request.GroupName}' but only {confirmed} were confirmed — the changes were rolled back.");
                }

                return confirmed;
            });
        }

        return new AddSelectedClientsToGroupResult(
            Applied: true,
            GroupName: request.GroupName,
            RequestedCount: requestedCount,
            FoundCount: clients.Count,
            NotFoundCount: notFound,
            EligibleCount: eligible.Count,
            AddedCount: newItems.Count,
            VerifiedCount: verified,
            AlreadyMemberCount: alreadyMember,
            Clients: eligibleItems);
    }

    private static ClientSearchItem ToSearchItem(Client client) => new()
    {
        Id = client.Id,
        FirstName = client.FirstName,
        LastName = client.Name,
        Company = client.Company,
        EntityType = client.Type.ToString()
    };
}
