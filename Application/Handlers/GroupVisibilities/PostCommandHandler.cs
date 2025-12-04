using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;
public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IGroupVisibilityRepository groupVisibilityRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupVisibilityResource?> Handle(PostCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var groupVisibility = _groupMapper.ToGroupVisibilityEntity(request.Resource);
        await _groupVisibilityRepository.Add(groupVisibility);
        await _unitOfWork.CompleteAsync();
        return _groupMapper.ToGroupVisibilityResource(groupVisibility);
    }
}