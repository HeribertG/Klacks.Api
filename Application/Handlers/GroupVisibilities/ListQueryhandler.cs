using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class ListQueryhandler : IRequestHandler<ListQuery<GroupVisibilityResource>, IEnumerable<GroupVisibilityResource>>
{
    private readonly IMapper mapper;
    private readonly IGroupVisibilityRepository repository;

    public ListQueryhandler(IMapper mapper,
                            IGroupVisibilityRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(ListQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var groupVisibilityList = await repository.GetGroupVisibilityList();
        return mapper.Map<IEnumerable<GroupVisibility>, IEnumerable<GroupVisibilityResource>>(groupVisibilityList);
    }
}
