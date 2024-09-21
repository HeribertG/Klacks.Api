using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Queries.Addresses;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Addresses
{
  public class GetClientAddressListQueryHandler : IRequestHandler<ClientAddressListQuery, IEnumerable<AddressResource>>
  {
    private readonly IMapper mapper;
    private readonly IAddressRepository repository;

    public GetClientAddressListQueryHandler(IMapper mapper,
                                       IAddressRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<AddressResource>> Handle(ClientAddressListQuery request, CancellationToken cancellationToken)
    {
      var address = await repository.ClienList(request.Id);
      return mapper.Map<List<Address>, List<AddressResource>>(address);
    }
  }
}
