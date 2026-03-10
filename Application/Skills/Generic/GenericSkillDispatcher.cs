// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dispatches generic skill execution based on HandlerType. Routes to the appropriate
/// executor (GenericListExecutor or GenericDeleteExecutor) and deserializes the handler config.
/// </summary>
/// <param name="listExecutor">Executor for generic list operations</param>
/// <param name="deleteExecutor">Executor for generic delete operations</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Skills.Generic;

public class GenericSkillDispatcher : IGenericSkillDispatcher
{
    private static readonly HashSet<string> SupportedHandlerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        HandlerTypes.GenericList,
        HandlerTypes.GenericDelete
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GenericListExecutor _listExecutor;
    private readonly GenericDeleteExecutor _deleteExecutor;
    private readonly ILogger<GenericSkillDispatcher> _logger;

    public GenericSkillDispatcher(
        GenericListExecutor listExecutor,
        GenericDeleteExecutor deleteExecutor,
        ILogger<GenericSkillDispatcher> logger)
    {
        _listExecutor = listExecutor;
        _deleteExecutor = deleteExecutor;
        _logger = logger;
    }

    public bool CanHandle(string? handlerType)
    {
        return !string.IsNullOrWhiteSpace(handlerType) && SupportedHandlerTypes.Contains(handlerType);
    }

    public async Task<SkillResult> ExecuteAsync(
        string handlerType,
        string handlerConfig,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching generic skill with HandlerType={HandlerType}", handlerType);

        return handlerType.ToLowerInvariant() switch
        {
            HandlerTypes.GenericList => await ExecuteListAsync(handlerConfig, parameters, cancellationToken),
            HandlerTypes.GenericDelete => await ExecuteDeleteAsync(handlerConfig, parameters, cancellationToken),
            _ => SkillResult.Error($"Unsupported handler type: '{handlerType}'")
        };
    }

    private async Task<SkillResult> ExecuteListAsync(
        string handlerConfig,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var config = DeserializeConfig<GenericListConfig>(handlerConfig);
        if (config == null)
            return SkillResult.Error("Failed to deserialize generic-list handler config.");

        return await _listExecutor.ExecuteAsync(config, parameters, cancellationToken);
    }

    private async Task<SkillResult> ExecuteDeleteAsync(
        string handlerConfig,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var config = DeserializeConfig<GenericDeleteConfig>(handlerConfig);
        if (config == null)
            return SkillResult.Error("Failed to deserialize generic-delete handler config.");

        return await _deleteExecutor.ExecuteAsync(config, parameters, cancellationToken);
    }

    private T? DeserializeConfig<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize handler config as {Type}", typeof(T).Name);
            return null;
        }
    }
}
