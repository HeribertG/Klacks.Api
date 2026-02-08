using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Addresses;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<AddressResource>, AddressResource>
{
    private readonly IAddressRepository _addressRepository;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;

    public GetQueryHandler(IAddressRepository addressRepository, AddressCommunicationMapper addressCommunicationMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
    }

    public async Task<AddressResource> Handle(GetQuery<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var address = await _addressRepository.Get(request.Id);

            if (address == null)
            {
                throw new KeyNotFoundException($"Address with ID {request.Id} not found");
            }

            return _addressCommunicationMapper.ToAddressResource(address);
        }, nameof(Handle), new { request.Id });
    }
}
