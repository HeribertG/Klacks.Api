// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="AddSelectedClientsToGroupCommand"/>. Loads the selected clients by id, skips
/// the ones already in the target group, and either previews the eligible clients (Apply=false) or adds
/// them in a single transaction that re-reads the new memberships and rolls back on a verification
/// mismatch. Mirrors the direct group_item persistence used by the criteria-fill handler, so re-running
/// it never creates duplicates.
/// </summary>
/// <param name="clientRepository">Loads the selected clients by id.</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits the new memberships in a single verified transaction.</param>
public sealed class AddSelectedClientsToGroupCommandHandler
    : IRequestHandler<AddSelectedClientsToGroupCommand, AddSelectedClientsToGroupResult>
{
    private readonly IClientRepository _clientRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddSelectedClientsToGroupCommandHandler(
        IClientRepository clientRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddSelectedClientsToGroupResult> Handle(
        AddSelectedClientsToGroupCommand request, CancellationToken cancellationToken)
    {
        var requestedCount = request.SelectedClientIds.Count;
        var clients = await _clientRepository.GetByIdsAsync(request.SelectedClientIds, cancellationToken);

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
        var validFrom = request.ValidFrom ?? now;
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
