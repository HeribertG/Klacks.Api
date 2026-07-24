// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed read-only repository for core-data quality scans. An address counts as active
/// when its ValidFrom is unset or not in the future; a contact counts when a non-empty
/// e-mail or phone communication row exists. Soft-deleted rows are excluded by the global
/// query filters.
/// </summary>

using Klacks.Api.Domain.DTOs.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ClientCoreDataReadRepository : IClientCoreDataReadRepository
{
    private static readonly CommunicationTypeEnum[] EmailTypes =
    [
        CommunicationTypeEnum.PrivateMail,
        CommunicationTypeEnum.OfficeMail
    ];

    private static readonly CommunicationTypeEnum[] PhoneTypes =
    [
        CommunicationTypeEnum.PrivateFixPhone,
        CommunicationTypeEnum.PrivateCellPhone,
        CommunicationTypeEnum.OfficeFixPhone,
        CommunicationTypeEnum.OfficeCellPhone,
        CommunicationTypeEnum.EmergencyPhone
    ];

    private readonly DataBaseContext _context;

    public ClientCoreDataReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<ClientCoreDataStatus>> GetActiveClientsWithMissingCoreDataAsync(
        DateOnly referenceDate,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var reference = referenceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await _context.Client
            .AsNoTracking()
            .Where(c => c.Membership != null
                && c.Membership.ValidFrom <= reference
                && (c.Membership.ValidUntil == null || c.Membership.ValidUntil >= reference))
            .Where(c => !c.Addresses.Any(a => a.ValidFrom == null || a.ValidFrom <= reference)
                || !c.Communications.Any(k => (EmailTypes.Contains(k.Type) || PhoneTypes.Contains(k.Type))
                    && k.Value != string.Empty))
            .OrderBy(c => c.Name)
            .ThenBy(c => c.FirstName)
            .Select(c => new ClientCoreDataStatus(
                c.Id,
                c.FirstName,
                c.Name,
                c.Addresses.Any(a => a.ValidFrom == null || a.ValidFrom <= reference),
                c.Communications.Any(k => (EmailTypes.Contains(k.Type) || PhoneTypes.Contains(k.Type))
                    && k.Value != string.Empty)))
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
