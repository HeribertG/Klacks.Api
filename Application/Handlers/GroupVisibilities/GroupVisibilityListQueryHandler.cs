using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Application.Queries.GroupVisibilities;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

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
        var groupVisibilityList = await repository.GroupVisibilityList(request.Id);
        return mapper.Map<IEnumerable<GroupVisibility>, IEnumerable<GroupVisibilityResource>>(groupVisibilityList);
    }
}
