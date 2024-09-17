using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Models.Staffs;
using Klacks_api.Queries.Addresses;
using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Handlers.Addresses
{
  public class GetSimpleAddressListQueryHandler : IRequestHandler<GetSimpleAddressListQuery, IEnumerable<AddressResource>>
  {
    private readonly IMapper mapper;
    private readonly IAddressRepository repository;

    public GetSimpleAddressListQueryHandler(IMapper mapper,
                                            IAddressRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<AddressResource>> Handle(GetSimpleAddressListQuery request, CancellationToken cancellationToken)
    {
      var address = await repository.SimpleList(request.Id);
      return mapper.Map<List<Address>, List<AddressResource>>(address);
    }
  }
}
