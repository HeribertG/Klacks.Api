// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

/// <summary>
/// EF Core repository for ERP drop points, including the poll timestamp written by the import runner.
/// </summary>
public class ErpDropPointRepository : BaseRepository<ErpDropPoint>, IErpDropPointRepository
{
    public ErpDropPointRepository(DataBaseContext context, ILogger<ErpDropPoint> logger)
        : base(context, logger)
    {
    }

    public async Task<int> MarkPolledAsync(Guid dropPointId, DateTime polledAtUtc, CancellationToken cancellationToken = default)
    {
        return await context.Set<ErpDropPoint>()
            .Where(d => d.Id == dropPointId && !d.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.LastPolledAt, polledAtUtc), cancellationToken);
    }
}
