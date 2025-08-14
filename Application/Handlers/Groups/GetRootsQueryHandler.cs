using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetRootsQueryHandler : IRequestHandler<GetRootsQuery, IEnumerable<GroupResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public GetRootsQueryHandler(IGroupRepository groupRepository, IMapper mapper)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<GroupResource>> Handle(GetRootsQuery request, CancellationToken cancellationToken)
    {
        var roots = await _groupRepository.GetRoots();
        return roots.Select(g => _mapper.Map<GroupResource>(g));
    }
}
