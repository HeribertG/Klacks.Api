// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        return await _unitOfWork.ExecuteInTransactionAsync(
            () => ExecuteAsync(operation, operationName, contextData));
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
