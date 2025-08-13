using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Repositories;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services;

public class ClientApplicationService
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;

    public ClientApplicationService(
        IClientRepository clientRepository, 
        IMapper mapper)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
    }

    public async Task<ClientResource?> GetClientByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.Get(id);
        return client != null ? _mapper.Map<ClientResource>(client) : null;
    }

    public async Task<TruncatedClientResource> SearchClientsAsync(FilterResource filter, CancellationToken cancellationToken = default)
    {
        var truncatedClient = await _clientRepository.Truncated(filter);
        return _mapper.Map<TruncatedClientResource>(truncatedClient);
    }

    public async Task<ClientResource> CreateClientAsync(ClientResource clientResource, CancellationToken cancellationToken = default)
    {
        var client = _mapper.Map<Client>(clientResource);
        await _clientRepository.Add(client);
        return _mapper.Map<ClientResource>(client);
    }

    public async Task<ClientResource> UpdateClientAsync(ClientResource clientResource, CancellationToken cancellationToken = default)
    {
        var client = _mapper.Map<Client>(clientResource);
        var updatedClient = await _clientRepository.Put(client);
        return _mapper.Map<ClientResource>(updatedClient);
    }

    public async Task DeleteClientAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _clientRepository.Delete(id);
    }

    public async Task<List<ClientResource>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.List();
        var activeClients = clients.Where(c => IsActiveClient(c)).ToList();
        return activeClients.Select(c => _mapper.Map<ClientResource>(c)).ToList();
    }

    public async Task<int> GetClientCountAsync(CancellationToken cancellationToken = default)
    {
        return _clientRepository.Count();
    }

    public async Task<IEnumerable<Client>> FindClientsAsync(string? company = null, string? name = null, string? firstName = null, CancellationToken cancellationToken = default)
    {
        return await _clientRepository.FindList(company, name, firstName);
    }

    public async Task<LastChangeMetaDataResource> GetLastChangeMetaDataAsync(CancellationToken cancellationToken = default)
    {
        return await _clientRepository.LastChangeMetaData();
    }

    public async Task<List<ClientBreakResource>> GetBreakListAsync(BreakFilter filter, CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.BreakList(filter);
        return _mapper.Map<List<ClientBreakResource>>(clients);
    }

    private bool IsActiveClient(Client client)
    {
        var today = DateTime.Now;
        return client.Membership != null && 
               client.Membership.ValidFrom <= today && 
               (client.Membership.ValidUntil == null || client.Membership.ValidUntil >= today);
    }
}


