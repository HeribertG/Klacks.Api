using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Queries.Addresses;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Addresses
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
