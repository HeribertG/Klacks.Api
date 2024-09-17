using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Handlers.Clients
{
  public class GetQueryHandler : IRequestHandler<GetQuery<ClientResource>, ClientResource>
  {
    private readonly IMapper mapper;
    private readonly IClientRepository repository;

    public GetQueryHandler(
                           IMapper mapper,
                           IClientRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<ClientResource> Handle(GetQuery<ClientResource> request, CancellationToken cancellationToken)
    {
      var client = await repository.Get(request.Id);
      return mapper.Map<Models.Staffs.Client, ClientResource>(client!);
    }
  }
}
