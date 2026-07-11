// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One-time, idempotent startup backfill of the client phonetic_tokens column: existing rows
/// (created before the column existed) get their Kölner Phonetik codes computed in batches.
/// Rows are updated via raw SQL so the audit fields (UpdateTime/CurrentUserUpdated) stay
/// untouched; rows whose name fields yield no codes are stamped with an empty string so the
/// backfill never revisits them. New and edited clients are covered by the DataBaseContext
/// save hook instead.
/// </summary>
/// <param name="context">Database context used for batched reads and raw-SQL updates</param>
/// <param name="logger">Reports how many clients were backfilled</param>

using Klacks.Api.Domain.Services.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class ClientPhoneticBackfillSeed
{
    private const int BatchSize = 500;

    private readonly DataBaseContext _context;
    private readonly ILogger<ClientPhoneticBackfillSeed> _logger;

    public ClientPhoneticBackfillSeed(DataBaseContext context, ILogger<ClientPhoneticBackfillSeed> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var total = 0;

        while (true)
        {
            var batch = await _context.Client
                .IgnoreQueryFilters()
                .Where(c => c.PhoneticTokens == null)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Name, c.FirstName, c.SecondName, c.MaidenName })
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var client in batch)
            {
                var tokens = ClientPhoneticTokenBuilder.Build(
                    client.Name, client.FirstName, client.SecondName, client.MaidenName) ?? string.Empty;
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE client SET phonetic_tokens = {tokens} WHERE id = {client.Id}",
                    cancellationToken);
            }

            total += batch.Count;
        }

        if (total > 0)
        {
            _logger.LogInformation("Client phonetic backfill: {Count} client(s) updated.", total);
        }
    }
}
