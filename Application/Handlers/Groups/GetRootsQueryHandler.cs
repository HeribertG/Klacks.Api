using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetRootsQueryHandler : IRequestHandler<GetRootsQuery, IEnumerable<GroupResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;

    public GetRootsQueryHandler(IGroupRepository groupRepository, GroupMapper groupMapper)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
    }
    
    public async Task<IEnumerable<GroupResource>> Handle(GetRootsQuery request, CancellationToken cancellationToken)
    {
        var roots = await _groupRepository.GetRoots();
        return roots.Select(g => _groupMapper.ToGroupResource(g));
    }
}
