using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Queries.GroupVisibilities;
using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.GroupVisibilities;

public class GroupVisibilityListQueryHandler : IRequestHandler<GroupVisibilityListQuery, IEnumerable<GroupVisibilityResource>>
{
    private readonly IMapper mapper;
    private readonly IGroupVisibilityRepository repository;

    public GroupVisibilityListQueryHandler(IMapper mapper,
                                           IGroupVisibilityRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(GroupVisibilityListQuery request, CancellationToken cancellationToken)
    {
        var address = await repository.GroupVisibilityList(request.Id);
        return mapper.Map<List<GroupVisibility>, List<GroupVisibilityResource>>(address);
    }
}
