using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class ListQueryhandler : IRequestHandler<ListQuery<GroupVisibilityResource>, IEnumerable<GroupVisibilityResource>>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;

    public ListQueryhandler(IGroupVisibilityRepository groupVisibilityRepository, IMapper mapper)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(ListQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var groupVisibilities = await _groupVisibilityRepository.List();
        return _mapper.Map<IEnumerable<GroupVisibilityResource>>(groupVisibilities);
    }
}
