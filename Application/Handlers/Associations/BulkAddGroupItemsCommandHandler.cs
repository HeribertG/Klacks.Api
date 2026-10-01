// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates several group items in one transaction and reads them back inside it. This endpoint exists
/// because the operation is genuinely atomic: linking a set of shifts to a group half-way leaves the
/// group in a state nobody asked for. A caller doing it as N separate requests could not roll back what
/// already succeeded, so the atomicity has to live here, on the server, rather than in the caller.
/// Every target group and every linked client must be visible to the caller; one hidden group or client
/// refuses the whole batch like a missing one, before anything is written.
/// </summary>
/// <param name="groupItemRepository">Persists the rows and confirms them by id</param>
/// <param name="unitOfWork">Owns the transaction the whole batch runs in</param>
/// <param name="logger">Structured log of the batch outcome</param>

using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Associations;

public class BulkAddGroupItemsCommandHandler
    : BaseHandler, IRequestHandler<BulkAddGroupItemsCommand, BulkGroupItemResponse>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public BulkAddGroupItemsCommandHandler(
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        ILogger<BulkAddGroupItemsCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<BulkGroupItemResponse> Handle(
        BulkAddGroupItemsCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var requested = command.Request.Items.ToList();
            if (requested.Count == 0)
            {
                return new BulkGroupItemResponse();
            }

            await EnsureCallerMayLinkAsync(requested, cancellationToken);

            var items = requested.Select(resource => new GroupItem
            {
                Id = resource.Id == Guid.Empty ? Guid.NewGuid() : resource.Id,
                ClientId = resource.ClientId,
                ShiftId = resource.ShiftId,
                GroupId = resource.GroupId,
                ValidFrom = resource.ValidFrom,
                ValidUntil = resource.ValidUntil
            }).ToList();

            var ids = items.Select(item => item.Id).ToList();

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var item in items)
                {
                    await _groupItemRepository.Add(item);
                }

                await _unitOfWork.CompleteAsync();

                var confirmed = await _groupItemRepository.CountExistingByIds(ids, cancellationToken);
                if (confirmed != items.Count)
                {
                    // Throwing inside the transaction is what rolls the whole batch back — reporting a
                    // partial count instead would leave the caller believing it can retry the rest.
                    throw new InvalidRequestException(
                        $"Expected {items.Count} new group links but only {confirmed} were confirmed; " +
                        "the batch was rolled back.");
                }

                _logger.LogInformation("Bulk-added {Count} group items to group {GroupId}",
                    confirmed, items[0].GroupId);

                return new BulkGroupItemResponse { AddedCount = confirmed, AddedIds = ids };
            });
        },
        "bulk-adding group items",
        new { ItemCount = command.Request.Items.Count });
    }

    private async Task EnsureCallerMayLinkAsync(
        IReadOnlyCollection<GroupItemResource> requested, CancellationToken cancellationToken)
    {
        var groupIds = requested.Select(item => item.GroupId).Distinct().ToList();
        if (!await _groupVisibilityGuard.AreAllGroupsVisibleAsync(groupIds, cancellationToken))
        {
            foreach (var groupId in groupIds)
            {
                if (!await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
                {
                    throw new KeyNotFoundException(string.Format(GroupItemVisibilityMessages.GroupNotFound, groupId));
                }
            }
        }

        var clientIds = requested
            .Where(item => item.ClientId.HasValue)
            .Select(item => item.ClientId!.Value)
            .ToList();
        await GroupMemberVisibilityCheck.EnsureAddedClientsVisibleAsync(_clientVisibilityGuard, clientIds, cancellationToken);
    }
}
