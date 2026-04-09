// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IEmployeeClientReader to the Core Client and Communication tables.
/// Returns only non-deleted clients of EntityType Employee, enriched with the strict
/// private contact channels PrivateCellPhone and PrivateMail (oldest entry wins on conflict).
/// </summary>
/// <param name="context">EF Core database context.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class EmployeeClientReaderBridge : IEmployeeClientReader
{
    private readonly DataBaseContext _context;

    public EmployeeClientReaderBridge(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmployeeClientInfo>> GetAllEmployeesAsync(CancellationToken ct = default)
    {
        var clients = await _context.Set<Client>()
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Type == EntityTypeEnum.Employee)
            .Select(c => new { c.Id, c.FirstName })
            .ToListAsync(ct);

        if (clients.Count == 0)
            return Array.Empty<EmployeeClientInfo>();

        var clientIds = clients.Select(c => c.Id).ToList();
        var contacts = await LoadContactsAsync(clientIds, ct);

        return clients
            .Select(c =>
            {
                contacts.TryGetValue(c.Id, out var pair);
                return new EmployeeClientInfo(c.Id, c.FirstName, pair.Phone, pair.Email);
            })
            .ToList();
    }

    public async Task<EmployeeClientInfo?> GetEmployeeAsync(Guid clientId, CancellationToken ct = default)
    {
        var client = await _context.Set<Client>()
            .AsNoTracking()
            .Where(c => c.Id == clientId && !c.IsDeleted && c.Type == EntityTypeEnum.Employee)
            .Select(c => new { c.Id, c.FirstName })
            .FirstOrDefaultAsync(ct);

        if (client == null)
            return null;

        var contacts = await LoadContactsAsync(new[] { clientId }, ct);
        contacts.TryGetValue(clientId, out var pair);
        return new EmployeeClientInfo(client.Id, client.FirstName, pair.Phone, pair.Email);
    }

    private async Task<Dictionary<Guid, (string? Phone, string? Email)>> LoadContactsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken ct)
    {
        var rows = await _context.Set<Communication>()
            .AsNoTracking()
            .Where(c => clientIds.Contains(c.ClientId)
                     && !c.IsDeleted
                     && (c.Type == CommunicationTypeEnum.PrivateCellPhone || c.Type == CommunicationTypeEnum.PrivateMail)
                     && c.Value != null
                     && c.Value != string.Empty)
            .Select(c => new { c.ClientId, c.Type, c.Prefix, c.Value, c.CreateTime })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, (string? Phone, string? Email)>();
        foreach (var group in rows.GroupBy(r => r.ClientId))
        {
            var phone = group
                .Where(r => r.Type == CommunicationTypeEnum.PrivateCellPhone)
                .OrderBy(r => r.CreateTime ?? DateTime.MaxValue)
                .Select(r => string.IsNullOrWhiteSpace(r.Prefix) ? r.Value : $"{r.Prefix}{r.Value}")
                .FirstOrDefault();

            var email = group
                .Where(r => r.Type == CommunicationTypeEnum.PrivateMail)
                .OrderBy(r => r.CreateTime ?? DateTime.MaxValue)
                .Select(r => r.Value)
                .FirstOrDefault();

            result[group.Key] = (
                string.IsNullOrWhiteSpace(phone) ? null : phone!.Trim(),
                string.IsNullOrWhiteSpace(email) ? null : email!.Trim());
        }
        return result;
    }
}
