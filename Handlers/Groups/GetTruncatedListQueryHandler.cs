using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Groups;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Handlers.Groups
{
  public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
  {
    private readonly IMapper mapper;
    private readonly IGroupRepository repository;

    public GetTruncatedListQueryHandler(
                                        IMapper mapper,
                                        IGroupRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<TruncatedGroupResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
      var truncated = await repository.Truncated(request.Filter);
      return mapper.Map<TruncatedGroup, TruncatedGroupResource>(truncated!);
    }
  }
}
