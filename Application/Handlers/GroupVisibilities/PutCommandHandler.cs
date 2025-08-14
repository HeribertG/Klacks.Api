using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class PutCommandHandler : IRequestHandler<PutCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IGroupVisibilityRepository groupVisibilityRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupVisibilityResource?> Handle(PutCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var existingGroupVisibility = await _groupVisibilityRepository.Get(request.Resource.Id);
        if (existingGroupVisibility == null)
        {
            return null;
        }

        _mapper.Map(request.Resource, existingGroupVisibility);
        await _groupVisibilityRepository.Put(existingGroupVisibility);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<GroupVisibilityResource>(existingGroupVisibility);
    }
}
