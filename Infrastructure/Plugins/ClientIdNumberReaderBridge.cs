// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bridges the Contracts IClientIdNumberReader to the main database context,
/// resolving active client GUIDs from their integer id numbers. Only messageable clients are returned
/// (ClientTypeRules.MessageableTypes), so a broadcast audience never contains a stand-in.
/// </summary>
/// <param name="context">EF Core database context</param>

using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class ClientIdNumberReaderBridge : IClientIdNumberReader
{
    private readonly DataBaseContext _context;

    public ClientIdNumberReaderBridge(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetClientIdsByIdNumbersAsync(
        IReadOnlyCollection<int> idNumbers,
        CancellationToken ct = default)
    {
        if (idNumbers.Count == 0)
            return Array.Empty<Guid>();

        var messageableTypes = ClientTypeRules.MessageableTypes.ToList();
        return await _context.Client
            .AsNoTracking()
            .Where(c => !c.IsDeleted && idNumbers.Contains(c.IdNumber) && messageableTypes.Contains(c.Type))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }
}
