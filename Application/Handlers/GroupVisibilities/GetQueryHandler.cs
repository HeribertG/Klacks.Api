using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GetQueryHandler : IRequestHandler<GetQuery<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;

    public GetQueryHandler(IGroupVisibilityRepository groupVisibilityRepository, IMapper mapper)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
    }

    public async Task<GroupVisibilityResource?> Handle(GetQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var groupVisibility = await _groupVisibilityRepository.Get(request.Id);
        return groupVisibility != null ? _mapper.Map<GroupVisibilityResource>(groupVisibility) : null;
    }
}
