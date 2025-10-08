using Microsoft.EntityFrameworkCore.Storage;

namespace Klacks.Api.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CompleteAsync();

        int Complete();

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task CommitTransactionAsync(IDbContextTransaction transaction);

        Task RollbackTransactionAsync(IDbContextTransaction transaction);
    }
}