// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="FillGroupByCriteriaCommand"/>. Searches clients by canton, contract and
/// entity type, then either previews the matches (Apply=false) or adds the ones that are not already
/// members to the target group, committing all new memberships in a single save. Mirrors the direct
/// group_item persistence used by the customer-grouping handler and skips clients that already belong
/// to the group, so re-running it never creates duplicates.
/// </summary>
/// <param name="searchRepository">Finds the clients matching the criteria.</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits the new memberships in a single save.</param>
public sealed class FillGroupByCriteriaCommandHandler
    : IRequestHandler<FillGroupByCriteriaCommand, FillGroupByCriteriaResult>
{
    private const int DefaultMatchLimit = 100;

    private readonly IClientSearchRepository _searchRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FillGroupByCriteriaCommandHandler(
        IClientSearchRepository searchRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _searchRepository = searchRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FillGroupByCriteriaResult> Handle(
        FillGroupByCriteriaCommand request, CancellationToken cancellationToken)
    {
        var limit = request.Count is > 0 ? request.Count.Value : DefaultMatchLimit;

        var search = await _searchRepository.SearchAsync(
            searchTerm: null,
            canton: request.Canton,
            entityType: request.EntityType,
            contractId: request.ContractId,
            limit: limit,
            cancellationToken: cancellationToken);

        var matched = search.Items;

        if (!request.Apply)
        {
            return new FillGroupByCriteriaResult(
                Applied: false,
                GroupName: request.GroupName,
                TotalMatchCount: search.TotalCount,
                AddedCount: 0,
                VerifiedCount: 0,
                AlreadyMemberCount: 0,
                Clients: matched);
        }

        var alreadyMember = 0;
        var now = DateTime.UtcNow;
        var validFrom = request.ValidFrom ?? now;
        var newItems = new List<GroupItem>();

        foreach (var client in matched)
        {
            var existing = await _groupItemRepository.GetByClientAndGroup(client.Id, request.GroupId);
            if (existing != null && !existing.IsDeleted)
            {
                alreadyMember++;
                continue;
            }

            newItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                GroupId = request.GroupId,
                ValidFrom = validFrom,
                CreateTime = now,
                CurrentUserCreated = request.UserName
            });
        }

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
                        "fill_group_by_criteria",
                        $"Database verification failed: expected {newItems.Count} new memberships in group " +
                        $"'{request.GroupName}' but only {confirmed} were confirmed — the changes were rolled back.");
                }

                return confirmed;
            });
        }

        return new FillGroupByCriteriaResult(
            Applied: true,
            GroupName: request.GroupName,
            TotalMatchCount: search.TotalCount,
            AddedCount: newItems.Count,
            VerifiedCount: verified,
            AlreadyMemberCount: alreadyMember,
            Clients: matched);
    }
}
