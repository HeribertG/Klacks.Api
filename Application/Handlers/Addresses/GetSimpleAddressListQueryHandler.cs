using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses
{
    public class GetSimpleAddressListQueryHandler : IRequestHandler<GetSimpleAddressListQuery, IEnumerable<AddressResource>>
    {
        private readonly AddressApplicationService _addressApplicationService;

        public GetSimpleAddressListQueryHandler(AddressApplicationService addressApplicationService)
        {
            _addressApplicationService = addressApplicationService;
        }

        public async Task<IEnumerable<AddressResource>> Handle(GetSimpleAddressListQuery request, CancellationToken cancellationToken)
        {
            return await _addressApplicationService.GetSimpleAddressListAsync(request.Id, cancellationToken);
        }
    }
}
