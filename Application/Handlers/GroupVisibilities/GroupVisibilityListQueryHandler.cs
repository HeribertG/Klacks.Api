using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.GroupVisibilities;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GroupVisibilityListQueryHandler : IRequestHandler<GroupVisibilityListQuery, IEnumerable<GroupVisibilityResource>>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;

    public GroupVisibilityListQueryHandler(IGroupVisibilityRepository groupVisibilityRepository, IMapper mapper)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(GroupVisibilityListQuery request, CancellationToken cancellationToken)
    {
        var groupVisibilities = await _groupVisibilityRepository.GroupVisibilityList(request.Id);
        return _mapper.Map<IEnumerable<GroupVisibilityResource>>(groupVisibilities);
    }
}
