using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses
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
