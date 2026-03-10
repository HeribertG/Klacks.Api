// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes delete operations for skills that are defined declaratively via GenericDeleteConfig.
/// Resolves the repository interface from DI, retrieves the entity via reflection, validates it exists,
/// deletes it, and optionally commits via IUnitOfWork.
/// @param serviceProvider - Used to resolve the target repository and IUnitOfWork at runtime
/// </summary>
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Generic;

public class GenericDeleteExecutor
{
    private static readonly HashSet<string> AllowedRepositories = new()
    {
        "IBranchRepository",
        "IContractRepository",
        "IGroupRepository",
        "ILLMRepository",
        "ISchedulingRuleRepository",
        "IAgentMemoryRepository"
    };

    private readonly IServiceProvider _serviceProvider;

    public GenericDeleteExecutor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<SkillResult> ExecuteAsync(
        GenericDeleteConfig config,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedRepositories.Contains(config.RepositoryInterface))
            return SkillResult.Error($"Repository '{config.RepositoryInterface}' is not allowed.");

        var interfaceType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.IsInterface && t.Name == config.RepositoryInterface);

        if (interfaceType == null)
            return SkillResult.Error($"Repository interface '{config.RepositoryInterface}' was not found.");

        var repository = _serviceProvider.GetRequiredService(interfaceType);

        if (!parameters.TryGetValue(config.IdParameter, out var idRaw) || idRaw == null)
            return SkillResult.Error($"Required parameter '{config.IdParameter}' is missing.");

        if (!Guid.TryParse(idRaw.ToString(), out var entityId))
            return SkillResult.Error($"Parameter '{config.IdParameter}' is not a valid GUID.");

        var getMethod = interfaceType.GetMethod(config.GetMethod);
        if (getMethod == null)
            return SkillResult.Error($"Method '{config.GetMethod}' not found on '{config.RepositoryInterface}'.");

        var getResult = getMethod.Invoke(repository, new object[] { entityId });
        if (getResult is Task getTask)
        {
            await getTask.ConfigureAwait(false);
            var resultProp = getTask.GetType().GetProperty("Result");
            var entity = resultProp?.GetValue(getTask);

            if (entity == null)
                return SkillResult.Error($"{config.EntityLabel} with ID '{entityId}' not found.");

            var entityName = entity.GetType().GetProperty(config.NameField)?.GetValue(entity)?.ToString()
                             ?? entityId.ToString();

            var deleteMethod = interfaceType.GetMethod(config.DeleteMethod);
            if (deleteMethod == null)
                return SkillResult.Error($"Method '{config.DeleteMethod}' not found on '{config.RepositoryInterface}'.");

            var deleteResult = deleteMethod.Invoke(repository, new object[] { entityId });
            if (deleteResult is Task deleteTask)
                await deleteTask.ConfigureAwait(false);

            if (config.RequiresUnitOfWork)
            {
                var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.CompleteAsync();
            }

            var resultData = new Dictionary<string, object?>
            {
                [$"{config.EntityLabel}Id"] = entityId.ToString(),
                [$"Deleted{config.EntityLabel}Name"] = entityName
            };

            return SkillResult.SuccessResult(resultData,
                $"{config.EntityLabel} '{entityName}' was successfully deleted.");
        }

        return SkillResult.Error($"Method '{config.GetMethod}' did not return a Task.");
    }
}
