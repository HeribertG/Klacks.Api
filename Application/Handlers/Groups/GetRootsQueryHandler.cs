using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Application.Queries.Groups;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetRootsQueryHandler : IRequestHandler<GetRootsQuery, IEnumerable<GroupResource>>
{
    private readonly IMapper mapper;
    private readonly IGroupRepository repository;

    public GetRootsQueryHandler(
                                IMapper mapper,
                                IGroupRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }
    public async Task<IEnumerable<GroupResource>> Handle(GetRootsQuery request, CancellationToken cancellationToken)
    {
        var list = await repository.GetRoots();
        return mapper.Map<IEnumerable<Models.Associations.Group>, IEnumerable<GroupResource>>(list);
    }
}
