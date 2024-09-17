using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Handlers.Addresses
{
  public class GetQueryHandler : IRequestHandler<GetQuery<AddressResource>, AddressResource>
  {
    private readonly IMapper mapper;
    private readonly IAddressRepository repository;

    public GetQueryHandler(IMapper mapper,
                           IAddressRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<AddressResource> Handle(GetQuery<AddressResource> request, CancellationToken cancellationToken)
    {
      var address = await repository.Get(request.Id);
      return mapper.Map<Models.Staffs.Address, AddressResource>(address!);
    }
  }
}
