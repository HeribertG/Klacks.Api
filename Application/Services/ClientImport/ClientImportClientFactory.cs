// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the client aggregate of a ready import row: the employee with membership, optional address,
/// e-mail/phone/mobile communications, contract, group membership and note. The entry date feeds the
/// membership start, the contract start, the group membership start and the address validity; the exit
/// date ends membership, contract and group membership. The IdNumber is left to the database sequence.
/// </summary>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportClientFactory
{
    private const int DefaultMembershipType = 0;

    public static Client Create(ClientImportDraft draft, ClientImportPolicy policy)
    {
        var clientId = Guid.NewGuid();
        var client = new Client
        {
            Id = clientId,
            FirstName = draft.FirstName,
            Name = draft.LastName!,
            Title = draft.Title ?? string.Empty,
            Gender = draft.Gender!.Value,
            Birthdate = draft.Birthdate,
            Type = EntityTypeEnum.Employee,
            LegalEntity = false,
            Membership = new Membership
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Type = DefaultMembershipType,
                ValidFrom = draft.EntryDate,
                ValidUntil = draft.ExitDate
            }
        };

        AddAddress(client, draft);
        AddCommunication(client, policy.EmailType, null, draft.Email);
        AddCommunication(client, policy.PhoneType, draft.PhonePrefix, draft.PhoneNumber);
        AddCommunication(client, policy.MobileType, draft.MobilePrefix, draft.MobileNumber);
        AddAssignments(client, draft);

        if (draft.Note != null)
        {
            client.Annotations.Add(new Annotation { Id = Guid.NewGuid(), ClientId = clientId, Note = draft.Note });
        }

        return client;
    }

    private static void AddAddress(Client client, ClientImportDraft draft)
    {
        if (!draft.HasAddress)
        {
            return;
        }

        client.Addresses.Add(new Address
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Type = AddressTypeEnum.Employee,
            ValidFrom = draft.EntryDate,
            Street = draft.Street ?? string.Empty,
            AddressLine2 = draft.AddressLine2 ?? string.Empty,
            Zip = draft.Zip ?? string.Empty,
            City = draft.City ?? string.Empty,
            State = draft.State ?? string.Empty,
            Country = draft.CountryCode ?? string.Empty
        });
    }

    private static void AddCommunication(Client client, CommunicationTypeEnum type, string? prefix, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        client.Communications.Add(new Communication
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Type = type,
            Prefix = prefix ?? string.Empty,
            Value = value
        });
    }

    private static void AddAssignments(Client client, ClientImportDraft draft)
    {
        if (draft.Contract != null)
        {
            client.ClientContracts.Add(new ClientContract
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                ContractId = draft.Contract.Id,
                FromDate = DateOnly.FromDateTime(draft.EntryDate),
                UntilDate = draft.ExitDate.HasValue ? DateOnly.FromDateTime(draft.ExitDate.Value) : null,
                IsActive = true
            });
        }

        if (draft.Group != null)
        {
            client.GroupItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                GroupId = draft.Group.Id,
                ValidFrom = draft.EntryDate,
                ValidUntil = draft.ExitDate
            });
        }
    }
}
