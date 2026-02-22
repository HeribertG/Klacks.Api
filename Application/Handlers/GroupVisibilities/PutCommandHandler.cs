using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IGroupVisibilityRepository groupVisibilityRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupVisibilityResource?> Handle(PutCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var existingGroupVisibility = await _groupVisibilityRepository.Get(request.Resource.Id);
        if (existingGroupVisibility == null)
        {
            return null;
        }

        var updatedGroupVisibility = _groupMapper.ToGroupVisibilityEntity(request.Resource);
        updatedGroupVisibility.CreateTime = existingGroupVisibility.CreateTime;
        updatedGroupVisibility.CurrentUserCreated = existingGroupVisibility.CurrentUserCreated;
        existingGroupVisibility = updatedGroupVisibility;
        await _groupVisibilityRepository.Put(existingGroupVisibility);
        await _unitOfWork.CompleteAsync();
        return _groupMapper.ToGroupVisibilityResource(existingGroupVisibility);
    }
}
