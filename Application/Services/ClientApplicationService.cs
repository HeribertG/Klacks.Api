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
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services;

/// <summary>
/// Application Service that orchestrates Client operations using Clean Architecture principles
/// Maps between Presentation DTOs and Domain Models/Criteria
/// </summary>
public class ClientApplicationService
{
    private readonly IClientRepository _clientRepository; // Current repository (Phase 1 already refactored)
    // private readonly IClientDomainRepository _clientDomainRepository; // Phase 2 - TODO: Implement
    private readonly IMapper _mapper;

    public ClientApplicationService(
        IClientRepository clientRepository, 
        // IClientDomainRepository clientDomainRepository, // Phase 2
        IMapper mapper)
    {
        _clientRepository = clientRepository;
        // _clientDomainRepository = clientDomainRepository; // Phase 2
        _mapper = mapper;
    }

    /// <summary>
    /// Get a single client by ID - demonstrates proper mapping
    /// </summary>
    public async Task<ClientResource?> GetClientByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // var client = await _clientDomainRepository.GetByIdAsync(id, cancellationToken);
        
        // Phase 1: Use existing repository
        var client = await _clientRepository.Get(id);
        
        return client != null ? _mapper.Map<ClientResource>(client) : null;
    }

    /// <summary>
    /// Search clients with filtering - demonstrates DTO to Criteria mapping
    /// </summary>
    public async Task<TruncatedClientResource> SearchClientsAsync(FilterResource filter, CancellationToken cancellationToken = default)
    {
        // Phase 3: Map Presentation Filter to Domain Criteria (TODO: Create ClientSearchCriteria)
        // var searchCriteria = _mapper.Map<ClientSearchCriteria>(filter);
        
        // TODO Phase 2: Use Domain Repository
        // var pagedResult = await _clientDomainRepository.SearchAsync(searchCriteria, cancellationToken);
        
        // Phase 1: Use existing repository (temporary) - repository already handles pagination
        var truncatedClient = await _clientRepository.Truncated(filter);
        
        // Map Domain to Presentation DTO
        return _mapper.Map<TruncatedClientResource>(truncatedClient);
    }

    /// <summary>
    /// Create a new client - demonstrates DTO to Domain Model mapping
    /// </summary>
    public async Task<ClientResource> CreateClientAsync(ClientResource clientResource, CancellationToken cancellationToken = default)
    {
        // Map Presentation DTO to Domain Model
        var client = _mapper.Map<Client>(clientResource);
        
        // Phase 2: Use Domain Repository
        // var createdClient = await _clientDomainRepository.AddAsync(client, cancellationToken);
        
        // Phase 1: Use existing repository
        await _clientRepository.Add(client);
        
        // Map back to Presentation DTO (client may have been updated with ID)
        return _mapper.Map<ClientResource>(client);
    }

    /// <summary>
    /// Update an existing client - demonstrates bidirectional mapping
    /// </summary>
    public async Task<ClientResource> UpdateClientAsync(ClientResource clientResource, CancellationToken cancellationToken = default)
    {
        // Map Presentation DTO to Domain Model
        var client = _mapper.Map<Client>(clientResource);
        
        // Phase 2: Use Domain Repository
        // var updatedClient = await _clientDomainRepository.UpdateAsync(client, cancellationToken);
        
        // Phase 1: Use existing repository
        var updatedClient = await _clientRepository.Put(client);
        
        // Map back to Presentation DTO
        return _mapper.Map<ClientResource>(updatedClient);
    }

    /// <summary>
    /// Delete a client by ID
    /// </summary>
    public async Task DeleteClientAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // await _clientDomainRepository.DeleteAsync(id, cancellationToken);
        
        // Phase 1: Use existing repository
        await _clientRepository.Delete(id);
    }

    /// <summary>
    /// Get active clients - demonstrates Domain-specific query
    /// </summary>
    public async Task<List<ClientResource>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // var activeClients = await _clientDomainRepository.GetActiveClientsAsync(cancellationToken);
        // return activeClients.Select(c => _mapper.Map<ClientResource>(c)).ToList();
        
        // Phase 1: Use existing repository with business logic
        var clients = await _clientRepository.List(); // This would need filtering logic
        
        // Apply domain logic here (temporary until Phase 2)
        var activeClients = clients.Where(c => IsActiveClient(c)).ToList();
        
        return activeClients.Select(c => _mapper.Map<ClientResource>(c)).ToList();
    }

    /// <summary>
    /// Temporary business logic until Phase 2 Domain Repository is implemented
    /// </summary>
    private bool IsActiveClient(Client client)
    {
        // This logic should be in Domain Services/Repository
        var today = DateTime.Now;
        return client.Membership != null && 
               client.Membership.ValidFrom <= today && 
               (client.Membership.ValidUntil == null || client.Membership.ValidUntil >= today);
    }
}


