// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the major version of the connected PostgreSQL server from server_version_num (e.g. 190000 → 19).
/// </summary>
/// <param name="context">The database context whose connection is asked</param>
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Update;

public class DatabaseEngineVersionReader : IDatabaseEngineVersionReader
{
    private const string ServerVersionNumSql = "SELECT current_setting('server_version_num')::int AS \"Value\"";
    private const int ServerVersionNumDivisor = 10000;

    private readonly DataBaseContext _context;

    public DatabaseEngineVersionReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<int> GetMajorVersionAsync(CancellationToken cancellationToken = default)
    {
        var versionNumbers = await _context.Database.SqlQueryRaw<int>(ServerVersionNumSql).ToListAsync(cancellationToken);
        return versionNumbers.Single() / ServerVersionNumDivisor;
    }
}
