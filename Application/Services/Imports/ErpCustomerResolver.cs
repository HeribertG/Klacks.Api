// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the Client a freshly imported ERP order belongs to: reuses an existing customer
/// (matched by external reference first, then by the existing Company+Zip+Street business key)
/// or creates a new one from the embedded customer payload.
/// </summary>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Imports;

public class ErpCustomerResolver
{
    private readonly IClientRepository _clientRepository;

    public ErpCustomerResolver(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<Client> ResolveAsync(ImportedCustomerPayload payload, CancellationToken cancellationToken = default)
    {
        var candidate = new Client
        {
            Type = EntityTypeEnum.Customer,
            Gender = GenderEnum.LegalEntity,
            LegalEntity = true,
            Company = payload.Company,
            SourceSystemId = payload.SourceSystemId,
            ExternalCustomerReference = string.IsNullOrWhiteSpace(payload.ExternalCustomerReference) ? null : payload.ExternalCustomerReference,
            Addresses =
            [
                new Address
                {
                    Type = AddressTypeEnum.Workplace,
                    ValidFrom = DateTime.UtcNow,
                    Street = payload.Street,
                    Zip = payload.Zip,
                    City = payload.City,
                    Country = payload.Country
                }
            ]
        };

        var existing = await _clientRepository.FindReusableCustomerAsync(candidate, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        candidate.Id = Guid.NewGuid();
        await _clientRepository.Add(candidate);
        return candidate;
    }
}
