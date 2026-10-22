// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
        catch (DbUpdateException ex)
        {
            throw DbUpdateExceptionTranslator.Translate(ex);
        }
    }

    public int Complete()
    {
        try
        {
            return context.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            throw DbUpdateExceptionTranslator.Translate(ex);
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

        try
        {
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
        catch (DbUpdateException ex)
        {
            throw DbUpdateExceptionTranslator.Translate(ex);
        }
    }

    public void DiscardTrackedChanges()
    {
        context.ChangeTracker.Clear();
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
