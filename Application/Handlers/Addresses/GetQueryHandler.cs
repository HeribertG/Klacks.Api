using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class GetQueryHandler : IRequestHandler<GetQuery<AddressResource>, AddressResource>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;

    public GetQueryHandler(IAddressRepository addressRepository, IMapper mapper)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
    }

    public async Task<AddressResource> Handle(GetQuery<AddressResource> request, CancellationToken cancellationToken)
    {
        var address = await _addressRepository.Get(request.Id);
        return address != null ? _mapper.Map<AddressResource>(address) : new AddressResource();
    }
}
