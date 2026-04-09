// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IClientEmailReader to the Core Communication table.
/// Returns the PrivateMail address of a client, oldest entry wins when multiple exist.
/// </summary>
/// <param name="context">EF Core database context.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class ClientEmailReaderBridge : IClientEmailReader
{
    private readonly DataBaseContext _context;

    public ClientEmailReaderBridge(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<string?> GetPrivateEmailAsync(Guid clientId, CancellationToken ct = default)
    {
        var value = await _context.Set<Communication>()
            .AsNoTracking()
            .Where(c => c.ClientId == clientId
                     && !c.IsDeleted
                     && c.Type == CommunicationTypeEnum.PrivateMail
                     && c.Value != null
                     && c.Value != string.Empty)
            .OrderBy(c => c.CreateTime ?? DateTime.MaxValue)
            .Select(c => c.Value)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
