using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Associations;
using MediatR;

namespace Klacks_api.Handlers.Memberships
{
  public class GetListQueryHandler : IRequestHandler<ListQuery<MembershipResource>, IEnumerable<MembershipResource>>
  {
    private readonly IMapper mapper;
    private readonly IMembershipRepository repository;

    public GetListQueryHandler(IMapper mapper,
                               IMembershipRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<MembershipResource>> Handle(ListQuery<MembershipResource> request, CancellationToken cancellationToken)
    {
      var memberships = await repository.List();

      return mapper.Map<List<Models.Associations.Membership>, List<MembershipResource>>(memberships!);
    }
  }
}
