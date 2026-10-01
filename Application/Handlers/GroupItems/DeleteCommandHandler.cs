// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.GroupItems;

/// <summary>
/// Removes one group link by id. A link of a group the caller cannot see is answered like a missing one and
/// is not removed.
/// </summary>
/// <param name="request">Id of the link to remove</param>
public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public DeleteCommandHandler(
        IGroupItemRepository groupItemRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<GroupItemResource?> Handle(DeleteCommand<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var stored = await _groupItemRepository.Get(request.Id);
            if (stored != null && !await _groupVisibilityGuard.IsGroupVisibleAsync(stored.GroupId, cancellationToken))
            {
                return null;
            }

            var groupItem = await _groupItemRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            if (groupItem == null)
            {
                return null;
            }
            return _groupMapper.ToGroupItemResource(groupItem);
        },
        "deleting group item",
        new { Id = request.Id });
    }
}
