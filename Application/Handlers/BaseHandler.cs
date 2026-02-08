using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers;

public abstract class BaseHandler
{
    protected readonly ILogger _logger;

    protected BaseHandler(ILogger logger)
    {
        _logger = logger;
    }

    protected async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationName,
        object? contextData = null)
    {
        try
        {
            return await operation();
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogWarning(ex, "Invalid request during {OperationName}. Context: {@ContextData}",
                operationName, contextData);
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found during {OperationName}. Context: {@ContextData}",
                operationName, contextData);
            throw;
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error during {OperationName}. Context: {@ContextData}",
                operationName, contextData);
            throw new InvalidRequestException(ex.Message);
        }
        catch (DatabaseUpdateException ex)
        {
            _logger.LogError(ex, "Database error during {OperationName}. Context: {@ContextData}",
                operationName, contextData);

            if (ex.IsDuplicate)
            {
                throw new InvalidRequestException($"A duplicate entry already exists. Operation: {operationName}");
            }

            if (ex.IsForeignKeyViolation)
            {
                throw new InvalidRequestException($"Referenced data not found or constraint violation. Operation: {operationName}. Detail: {ex.Message}");
            }

            throw new InvalidRequestException($"Database constraint violation during {operationName}. Detail: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Operation cancelled: {OperationName}", operationName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {OperationName}. Context: {@ContextData}. Exception: {ExceptionType}, Message: {ExceptionMessage}",
                operationName, contextData, ex.GetType().Name, ex.Message);
            throw new InvalidRequestException($"An unexpected error occurred during {operationName}: {ex.Message}");
        }
    }

    protected async Task ExecuteAsync(
        Func<Task> operation,
        string operationName,
        object? contextData = null)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, operationName, contextData);
    }
}
