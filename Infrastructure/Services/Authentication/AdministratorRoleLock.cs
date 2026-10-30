// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// PostgreSQL implementation of <see cref="IAdministratorRoleLock"/> using a transaction-scoped advisory lock, which
/// the database releases on commit or rollback. Without an open transaction that lock would end with its own statement
/// and protect nothing, so such a call is refused. Providers other than Npgsql (the in-memory test store) have no such
/// lock and do nothing.
/// </summary>
/// <param name="context">The scoped context whose current transaction owns the lock</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Authentication;

public sealed class AdministratorRoleLock : IAdministratorRoleLock
{
    private const long LockKey = 7203041955100001;
    private const string NoTransactionMessage =
        "The administrator-role lock lives as long as a transaction and was requested outside of one.";

    private readonly DataBaseContext _context;

    public AdministratorRoleLock(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(CancellationToken cancellationToken)
    {
        if (!_context.Database.IsNpgsql())
        {
            return;
        }

        if (_context.Database.CurrentTransaction == null)
        {
            throw new InvalidOperationException(NoTransactionMessage);
        }

        await _context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);
    }
}
