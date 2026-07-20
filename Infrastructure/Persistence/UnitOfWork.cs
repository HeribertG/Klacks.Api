// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Klacks.Api.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    // Postgres SQLSTATE codes (locale-independent, unlike ex.Message which is
    // localized by the server's lc_messages setting — e.g. "doppelter
    // Schlüsselwert" on a German-locale server never contains "duplicate").
    private const string UniqueViolationSqlState = "23505";
    private const string ForeignKeyViolationSqlState = "23503";

    private readonly DataBaseContext context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(DataBaseContext context, ILogger<UnitOfWork> logger)
    {
        this.context = context;
        this._logger = logger;
    }

    public async Task CompleteAsync()
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "The record was modified by another user. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var sqlState = (ex.InnerException as PostgresException)?.SqlState;
            var isDuplicate = sqlState == UniqueViolationSqlState;
            var isForeignKey = sqlState == ForeignKeyViolationSqlState;

            throw new DatabaseUpdateException(innerMessage, ex,
                isDuplicate: isDuplicate, isForeignKeyViolation: isForeignKey);
        }
    }

    public int Complete()
    {
        try
        {
            return context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "The record was modified by another user. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var sqlState = (ex.InnerException as PostgresException)?.SqlState;
            var isDuplicate = sqlState == UniqueViolationSqlState;
            var isForeignKey = sqlState == ForeignKeyViolationSqlState;

            throw new DatabaseUpdateException(innerMessage, ex,
                isDuplicate: isDuplicate, isForeignKeyViolation: isForeignKey);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync()
    {
        var dbTransaction = await context.Database.BeginTransactionAsync();
        return new TransactionWrapper(dbTransaction);
    }

    public async Task CommitTransactionAsync(ITransaction transaction)
    {
        if (transaction is TransactionWrapper wrapper)
        {
            await wrapper.Inner.CommitAsync();
        }
    }

    public async Task RollbackTransactionAsync(ITransaction transaction)
    {
        if (transaction is TransactionWrapper wrapper)
        {
            await wrapper.Inner.RollbackAsync();
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await operation();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private sealed class TransactionWrapper : ITransaction
    {
        public IDbContextTransaction Inner { get; }

        public TransactionWrapper(IDbContextTransaction inner)
        {
            Inner = inner;
        }

        public async ValueTask DisposeAsync()
        {
            await Inner.DisposeAsync();
        }

        public void Dispose()
        {
            Inner.Dispose();
        }
    }
}
