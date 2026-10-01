// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Soft-deletes a single group. A group-restricted caller may only delete a visible group; a hidden one is
/// answered exactly like a missing one, before anything is written.
/// </summary>
/// <param name="groupRepository">Self-committing group store</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the group</param>
public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupResource>, GroupResource?>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found.";

    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public DeleteCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupVisibilityGuard groupVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupVisibilityGuard = groupVisibilityGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupResource?> Handle(DeleteCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.Id, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, request.Id));
            }

            var group = await _groupRepository.Get(request.Id);
            if (group == null)
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, request.Id));
            }

            var groupToDelete = _groupMapper.ToGroupResource(group);
            await _groupRepository.Delete(request.Id);

            await _unitOfWork.CompleteAsync();

            return groupToDelete;
        }, 
        "deleting group", 
        new { GroupId = request.Id });
    }
}
