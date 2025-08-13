using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses
{
    public class GetClientAddressListQueryHandler : IRequestHandler<ClientAddressListQuery, IEnumerable<AddressResource>>
    {
        private readonly AddressApplicationService _addressApplicationService;

        public GetClientAddressListQueryHandler(AddressApplicationService addressApplicationService)
        {
            _addressApplicationService = addressApplicationService;
        }

        public async Task<IEnumerable<AddressResource>> Handle(ClientAddressListQuery request, CancellationToken cancellationToken)
        {
            return await _addressApplicationService.GetClientAddressListAsync(request.Id, cancellationToken);
        }
    }
}
