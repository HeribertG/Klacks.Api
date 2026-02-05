using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IGroupVisibilityRepository groupVisibilityRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupVisibilityResource?> Handle(DeleteCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var existingGroupVisibility = await _groupVisibilityRepository.Get(request.Id);
        if (existingGroupVisibility == null)
        {
            return null;
        }

        var groupVisibilityResource = _groupMapper.ToGroupVisibilityResource(existingGroupVisibility);
        await _groupVisibilityRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return groupVisibilityResource;
    }
}
