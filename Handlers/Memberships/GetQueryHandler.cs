using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Associations;
using MediatR;

namespace Klacks_api.Handlers.Memberships
{
  public class GetQueryHandler : IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
  {
    private readonly IMapper mapper;
    private readonly IMembershipRepository repository;

    public GetQueryHandler(IMapper mapper,
                           IMembershipRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
    {
      var membership = await repository.Get(request.Id);
      return mapper.Map<Models.Associations.Membership, MembershipResource>(membership!);
    }
  }
}
