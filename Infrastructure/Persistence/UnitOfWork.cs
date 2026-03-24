// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Klacks.Api.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
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
            var isDuplicate = innerMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
            var isForeignKey = innerMessage.Contains("foreign key", StringComparison.OrdinalIgnoreCase);

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
            var isDuplicate = innerMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
            var isForeignKey = innerMessage.Contains("foreign key", StringComparison.OrdinalIgnoreCase);

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
