using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses
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
