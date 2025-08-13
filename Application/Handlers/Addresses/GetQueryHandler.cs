using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class GetQueryHandler : IRequestHandler<GetQuery<AddressResource>, AddressResource>
{
    private readonly AddressApplicationService _addressApplicationService;

    public GetQueryHandler(AddressApplicationService addressApplicationService)
    {
        _addressApplicationService = addressApplicationService;
    }

    public async Task<AddressResource> Handle(GetQuery<AddressResource> request, CancellationToken cancellationToken)
    {
        return await _addressApplicationService.GetAddressByIdAsync(request.Id, cancellationToken) ?? new AddressResource();
    }
}
