// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a group together with its children in one transaction. The plain group DELETE removes
/// exactly one row, so a caller wanting the subtree gone would have to issue one request per child and
/// could not roll back the ones that already succeeded — a half-deleted tree is worse than no deletion
/// at all. The atomicity therefore lives here. A group-restricted caller may only delete a visible group
/// (its children are visible through the same root); a hidden one is answered exactly like a missing one.
/// </summary>
/// <param name="groupRepository">Reads the children and removes the rows</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the group</param>
/// <param name="unitOfWork">Owns the transaction the whole subtree removal runs in</param>
/// <param name="logger">Structured log of the outcome</param>

using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Associations;

public class DeleteGroupSubtreeCommandHandler
    : BaseHandler, IRequestHandler<DeleteGroupSubtreeCommand, DeleteGroupSubtreeResponse>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found.";

    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public DeleteGroupSubtreeCommandHandler(
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<DeleteGroupSubtreeCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteGroupSubtreeResponse> Handle(
        DeleteGroupSubtreeCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(command.Id, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, command.Id));
            }

            var group = await _groupRepository.Get(command.Id)
                ?? throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, command.Id));

            var groupName = group.Name;
            var children = (await _groupRepository.GetChildren(command.Id)).ToList();

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var child in children)
                {
                    await _groupRepository.Delete(child.Id);
                }

                await _groupRepository.Delete(command.Id);
                await _unitOfWork.CompleteAsync();

                var deleted = children.Count + 1;
                _logger.LogInformation(
                    "Deleted group {GroupId} together with {ChildCount} child group(s)",
                    command.Id, children.Count);

                return new DeleteGroupSubtreeResponse { DeletedGroupName = groupName, DeletedCount = deleted };
            });
        },
        "deleting a group subtree",
        new { GroupId = command.Id });
    }
}
