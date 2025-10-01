using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers;

/// <summary>
/// Base class for handlers that require database transactions with standardized error handling
/// </summary>
public abstract class BaseTransactionHandler : BaseHandler
{
    protected readonly IUnitOfWork _unitOfWork;

    protected BaseTransactionHandler(IUnitOfWork unitOfWork, ILogger logger)
        : base(logger)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executes an operation within a database transaction with standardized error handling
    /// </summary>
    protected async Task<TResult> ExecuteWithTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationName,
        object? contextData = null)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
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

    /// <summary>
    /// Executes an operation within a database transaction without a return value
    /// </summary>
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