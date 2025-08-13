using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class AddressApplicationService
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AddressApplicationService> _logger;

    public AddressApplicationService(
        IAddressRepository addressRepository,
        IMapper mapper,
        ILogger<AddressApplicationService> logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AddressResource?> GetAddressByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var address = await _addressRepository.Get(id);
        return address != null ? _mapper.Map<AddressResource>(address) : null;
    }

    public async Task<List<AddressResource>> GetClientAddressListAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var addresses = await _addressRepository.ClienList(clientId);
        return _mapper.Map<List<AddressResource>>(addresses);
    }

    public async Task<List<AddressResource>> GetSimpleAddressListAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var addresses = await _addressRepository.SimpleList(clientId);
        return _mapper.Map<List<AddressResource>>(addresses);
    }

    public async Task<AddressResource> CreateAddressAsync(AddressResource addressResource, CancellationToken cancellationToken = default)
    {
        var address = _mapper.Map<Address>(addressResource);
        await _addressRepository.Add(address);
        return _mapper.Map<AddressResource>(address);
    }

    public async Task<AddressResource> UpdateAddressAsync(AddressResource addressResource, CancellationToken cancellationToken = default)
    {
        var address = _mapper.Map<Address>(addressResource);
        var updatedAddress = await _addressRepository.Put(address);
        return _mapper.Map<AddressResource>(updatedAddress);
    }

    public async Task DeleteAddressAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _addressRepository.Delete(id);
    }
}