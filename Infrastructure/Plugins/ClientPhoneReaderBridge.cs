// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IClientPhoneReader to the Core Communication table.
/// Returns the preferred mobile phone number of a client following the tie-break rules
/// PrivateCellPhone &gt; OfficeCellPhone, oldest entry wins. EmergencyPhone is excluded.
/// When a Prefix is stored alongside the Value, both are concatenated.
/// </summary>
/// <param name="context">EF Core database context</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class ClientPhoneReaderBridge : IClientPhoneReader
{
    private static readonly CommunicationTypeEnum[] MobileTypes =
    {
        CommunicationTypeEnum.PrivateCellPhone,
        CommunicationTypeEnum.OfficeCellPhone,
    };

    private readonly DataBaseContext _context;

    public ClientPhoneReaderBridge(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<string?> GetMobilePhoneAsync(Guid clientId, CancellationToken ct = default)
    {
        var candidates = await LoadCandidatesAsync(new[] { clientId }, ct);
        return candidates.TryGetValue(clientId, out var value) ? value : null;
    }

    public async Task<IReadOnlyDictionary<Guid, string?>> GetMobilePhonesAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken ct = default)
    {
        if (clientIds == null || clientIds.Count == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        var found = await LoadCandidatesAsync(clientIds, ct);
        var result = new Dictionary<Guid, string?>(clientIds.Count);
        foreach (var id in clientIds)
        {
            result[id] = found.TryGetValue(id, out var value) ? value : null;
        }
        return result;
    }

    private async Task<Dictionary<Guid, string>> LoadCandidatesAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken ct)
    {
        var rows = await _context.Set<Communication>()
            .AsNoTracking()
            .Where(c => clientIds.Contains(c.ClientId)
                     && !c.IsDeleted
                     && MobileTypes.Contains(c.Type))
            .Select(c => new { c.ClientId, c.Type, c.Prefix, c.Value, c.CreateTime })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, string>();
        foreach (var group in rows.GroupBy(r => r.ClientId))
        {
            var best = group
                .OrderBy(r => r.Type == CommunicationTypeEnum.PrivateCellPhone ? 0 : 1)
                .ThenBy(r => r.CreateTime ?? DateTime.MaxValue)
                .First();

            var combined = string.IsNullOrWhiteSpace(best.Prefix)
                ? best.Value
                : $"{best.Prefix}{best.Value}";

            combined = combined?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(combined))
            {
                result[group.Key] = combined;
            }
        }
        return result;
    }
}
