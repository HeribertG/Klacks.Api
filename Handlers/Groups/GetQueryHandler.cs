using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Associations;
using MediatR;

namespace Klacks_api.Handlers.Groups
{
  public class GetQueryHandler : IRequestHandler<GetQuery<GroupResource>, GroupResource?>
  {
    private readonly IMapper mapper;
    private readonly IGroupRepository repository;

    public GetQueryHandler(
                           IMapper mapper,
                           IGroupRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<GroupResource?> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
    {
      var group = await repository.Get(request.Id);
      if (group != null)
      {
        return mapper.Map<Models.Associations.Group, GroupResource>(group);
      }

      return null;
    }
  }
}
