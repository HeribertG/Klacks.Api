using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;
public class PostCommandHandler : IRequestHandler<PostCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IGroupVisibilityRepository groupVisibilityRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupVisibilityResource?> Handle(PostCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var groupVisibility = _mapper.Map<GroupVisibility>(request.Resource);
        await _groupVisibilityRepository.Add(groupVisibility);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<GroupVisibilityResource>(groupVisibility);
    }
}