// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Groups;

public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IGroupGeocodingQueue _groupGeocodingQueue;

    public PostCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupGeocodingQueue groupGeocodingQueue,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupGeocodingQueue = groupGeocodingQueue;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        var created = await ExecuteWithTransactionAsync(async () =>
        {
            var group = _groupMapper.ToGroupEntity(request.Resource);
            group.Id = Guid.NewGuid();
            await _groupRepository.Add(group);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToGroupResource(group);
        },
        "creating group",
        new { GroupId = request.Resource?.Id });

        if (created != null)
        {
            _groupGeocodingQueue.Queue(created.Id);
        }

        return created;
    }
}