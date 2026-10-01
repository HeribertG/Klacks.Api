// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persists the metadata record of a committed employee import and answers whether a token was
/// already committed.
/// </summary>
/// <param name="context">Database context the batch is staged in</param>

using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

public class ClientImportBatchRepository : IClientImportBatchRepository
{
    private readonly DataBaseContext _context;

    public ClientImportBatchRepository(DataBaseContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByTokenAsync(Guid token, CancellationToken cancellationToken) =>
        _context.ClientImportBatches.IgnoreQueryFilters().AnyAsync(b => b.Token == token, cancellationToken);

    public async Task AddAsync(ClientImportBatch batch, CancellationToken cancellationToken)
    {
        await _context.ClientImportBatches.AddAsync(batch, cancellationToken);
    }
}
