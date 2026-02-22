namespace Klacks.Api.Domain.Interfaces;

public interface IUnitOfWork
{
    Task CompleteAsync();

    int Complete();

    Task<ITransaction> BeginTransactionAsync();

    Task CommitTransactionAsync(ITransaction transaction);

    Task RollbackTransactionAsync(ITransaction transaction);
}
