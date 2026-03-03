// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Groups;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<GroupResource?> Handle(PutCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var group = _groupMapper.ToGroupEntity(request.Resource);
            var updatedGroup = await _groupRepository.Put(group);
            var result = _groupMapper.ToGroupResource(updatedGroup);

            await _unitOfWork.CompleteAsync();

            return result;
        }, 
        "updating", 
        new { });
    }
}
