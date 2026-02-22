using Klacks.Api.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers;

public abstract class BaseTransactionHandler : BaseHandler
{
    protected readonly IUnitOfWork _unitOfWork;

    protected BaseTransactionHandler(IUnitOfWork unitOfWork, ILogger logger)
        : base(logger)
    {
        _unitOfWork = unitOfWork;
    }

    protected async Task<TResult> ExecuteWithTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationName,
        object? contextData = null)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var result = await ExecuteAsync(operation, operationName, contextData);
            await _unitOfWork.CommitTransactionAsync(transaction);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }

    protected async Task ExecuteWithTransactionAsync(
        Func<Task> operation,
        string operationName,
        object? contextData = null)
    {
        await ExecuteWithTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, operationName, contextData);
    }
}
