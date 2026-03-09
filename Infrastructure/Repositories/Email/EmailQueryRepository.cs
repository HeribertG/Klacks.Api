// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository-Implementierung fuer komplexe Email-Abfragen ueber mehrere Entitaeten.
/// @param context - Der Datenbank-Kontext fuer Entity Framework Zugriffe
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Email;

public class EmailQueryRepository : IEmailQueryRepository
{
    private readonly DataBaseContext _context;

    public EmailQueryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetDistinctAssignedEmailAddressesAsync(string folder, CancellationToken cancellationToken = default)
    {
        return await _context.ReceivedEmails
            .Where(e => !e.IsDeleted && e.Folder == folder)
            .Select(e => e.FromAddress.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ClientEmailInfo>> GetClientsWithEmailCommunicationsAsync(CancellationToken cancellationToken = default)
    {
        var communications = await _context.Set<Domain.Models.Staffs.Communication>()
            .Where(c => !c.IsDeleted &&
                        (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                        !string.IsNullOrEmpty(c.Value))
            .Include(c => c.Client)
            .ThenInclude(cl => cl.GroupItems)
            .Where(c => !c.Client.IsDeleted)
            .ToListAsync(cancellationToken);

        return communications.Select(c => new ClientEmailInfo
        {
            ClientId = c.ClientId,
            EmailAddress = c.Value,
            ClientDisplayName = $"{c.Client.IdNumber}, {c.Client.FirstName}, {c.Client.Name}".TrimEnd(',', ' '),
            GroupIds = c.Client.GroupItems.Where(gi => !gi.IsDeleted).Select(gi => gi.GroupId).ToList()
        }).ToList();
    }

    public async Task<int> CountEmailsByAddressAsync(string folder, string emailAddress, CancellationToken cancellationToken = default)
    {
        return await _context.ReceivedEmails
            .CountAsync(e => !e.IsDeleted &&
                             e.Folder == folder &&
                             e.FromAddress.ToLower() == emailAddress.ToLower(),
                cancellationToken);
    }

    public async Task<int> CountUnreadEmailsByAddressAsync(string folder, string emailAddress, CancellationToken cancellationToken = default)
    {
        return await _context.ReceivedEmails
            .CountAsync(e => !e.IsDeleted &&
                             e.Folder == folder &&
                             !e.IsRead &&
                             e.FromAddress.ToLower() == emailAddress.ToLower(),
                cancellationToken);
    }

    public async Task<List<string>> GetEmailAddressesByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Domain.Models.Staffs.Communication>()
            .Where(c => !c.IsDeleted &&
                        c.ClientId == clientId &&
                        (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                        !string.IsNullOrEmpty(c.Value))
            .Select(c => c.Value.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetClientIdsByGroupIdsAsync(HashSet<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        return await _context.GroupItem
            .Where(gi => !gi.IsDeleted && gi.ClientId.HasValue && groupIds.Contains(gi.GroupId))
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetEmailAddressesByClientIdsAsync(List<Guid> clientIds, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Domain.Models.Staffs.Communication>()
            .Where(c => !c.IsDeleted &&
                        clientIds.Contains(c.ClientId) &&
                        (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                        !string.IsNullOrEmpty(c.Value))
            .Select(c => c.Value.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<ReceivedEmailQueryResult> GetEmailsByAddressesAsync(
        string folder,
        List<string> emailAddresses,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReceivedEmails
            .Where(e => !e.IsDeleted &&
                        e.Folder == folder &&
                        emailAddresses.Contains(e.FromAddress.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await query.CountAsync(e => !e.IsRead, cancellationToken);

        var emails = await query
            .OrderByDescending(e => e.ReceivedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new ReceivedEmailQueryResult
        {
            Items = emails,
            TotalCount = totalCount,
            UnreadCount = unreadCount
        };
    }
}
