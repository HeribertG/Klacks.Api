using Klacks.Api.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Klacks.Api.Datas;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataBaseContext context;
    private readonly ILogger<UnitOfWork> logger;

    public UnitOfWork(DataBaseContext context, ILogger<UnitOfWork> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task CompleteAsync()
    {
        await context.SaveChangesAsync();
    }

    public int Complete()
    {
        return context.SaveChanges();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        await transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
    {
        await transaction.RollbackAsync();
    }
}