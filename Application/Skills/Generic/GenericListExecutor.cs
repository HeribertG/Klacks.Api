// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes list operations for skills that are defined declaratively via GenericListConfig.
/// Resolves the repository interface from DI, invokes the configured method via reflection,
/// applies optional IsDeleted filtering, search-term filtering, additional parameter filters, ordering, and field projection.
/// @param serviceProvider - Used to resolve the target repository at runtime
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Generic;

public class GenericListExecutor
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

    public GenericListExecutor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<SkillResult> ExecuteAsync(
        GenericListConfig config,
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

        var method = interfaceType.GetMethod(config.Method);
        if (method == null)
            return SkillResult.Error($"Method '{config.Method}' not found on '{config.RepositoryInterface}'.");

        var invocationResult = method.Invoke(repository, null);
        if (invocationResult is not Task task)
            return SkillResult.Error($"Method '{config.Method}' did not return a Task.");

        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var rawResult = resultProperty?.GetValue(task);

        if (rawResult is not System.Collections.IEnumerable rawEnumerable)
            return SkillResult.Error($"Method '{config.Method}' did not return an enumerable result.");

        var items = rawEnumerable.Cast<object>().ToList();

        if (config.FilterIsDeleted)
            items = FilterIsDeleted(items);

        var searchTerm = GetStringParameter(parameters, "searchTerm");
        if (!string.IsNullOrEmpty(searchTerm))
            items = FilterBySearchTerm(items, config, searchTerm);

        items = ApplyAdditionalFilters(items, config, parameters);

        items = ApplyOrdering(items, config);

        List<object> projected;
        if (config.Select.Count > 0)
            projected = items.Select(item => ProjectFields(item, config.Select)).ToList<object>();
        else
            projected = items;

        var resultData = BuildResult(projected, config.ResultProperty);

        var message = $"Found {projected.Count} item(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }

    private static List<object> FilterBySearchTerm(List<object> items, GenericListConfig config, string searchTerm)
    {
        if (config.SearchFields.Count > 0)
            return FilterByMultipleSearchFields(items, config.SearchFields, searchTerm);

        if (!string.IsNullOrEmpty(config.SearchField))
            return FilterBySearch(items, config.SearchField, searchTerm);

        return items;
    }

    private static List<object> FilterByMultipleSearchFields(List<object> items, List<string> fieldNames, string searchTerm)
    {
        return items.Where(item =>
        {
            var type = item.GetType();
            return fieldNames.Any(fieldName =>
            {
                var prop = type.GetProperty(fieldName);
                if (prop == null) return false;
                var value = prop.GetValue(item)?.ToString();
                return value != null && value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            });
        }).ToList();
    }

    private static List<object> ApplyOrdering(List<object> items, GenericListConfig config)
    {
        if (config.OrderByFields.Count > 0)
            return SortByMultipleFields(items, config.OrderByFields);

        if (!string.IsNullOrEmpty(config.OrderBy))
            return SortBy(items, config.OrderBy);

        return items;
    }

    private static List<object> SortByMultipleFields(List<object> items, List<string> fieldNames)
    {
        if (fieldNames.Count == 0)
            return items;

        var ordered = items.OrderBy(item => GetPropertyValue(item, fieldNames[0]));

        for (var i = 1; i < fieldNames.Count; i++)
        {
            var fieldName = fieldNames[i];
            ordered = ordered.ThenBy(item => GetPropertyValue(item, fieldName));
        }

        return ordered.ToList();
    }

    private static string GetPropertyValue(object item, string fieldName)
    {
        var prop = item.GetType().GetProperty(fieldName);
        return prop?.GetValue(item)?.ToString() ?? string.Empty;
    }

    private static List<object> ApplyAdditionalFilters(List<object> items, GenericListConfig config, Dictionary<string, object> parameters)
    {
        foreach (var filter in config.AdditionalFilters)
        {
            var paramValue = GetStringParameter(parameters, filter.ParameterName);
            if (string.IsNullOrEmpty(paramValue))
                continue;

            items = filter.MatchType.ToLowerInvariant() switch
            {
                "contains" => items.Where(item =>
                {
                    var prop = item.GetType().GetProperty(filter.PropertyName);
                    if (prop == null) return false;
                    var value = prop.GetValue(item)?.ToString();
                    return value != null && value.Contains(paramValue, StringComparison.OrdinalIgnoreCase);
                }).ToList(),
                _ => items.Where(item =>
                {
                    var prop = item.GetType().GetProperty(filter.PropertyName);
                    if (prop == null) return false;
                    var value = prop.GetValue(item)?.ToString();
                    return value != null && value.Equals(paramValue, StringComparison.OrdinalIgnoreCase);
                }).ToList()
            };
        }

        return items;
    }

    private static List<object> FilterIsDeleted(List<object> items)
    {
        return items.Where(item =>
        {
            var prop = item.GetType().GetProperty("IsDeleted");
            if (prop == null) return true;
            var value = prop.GetValue(item);
            return value is bool b && !b;
        }).ToList();
    }

    private static List<object> FilterBySearch(List<object> items, string fieldName, string searchTerm)
    {
        return items.Where(item =>
        {
            var prop = item.GetType().GetProperty(fieldName);
            if (prop == null) return false;
            var value = prop.GetValue(item)?.ToString();
            return value != null && value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }

    private static List<object> SortBy(List<object> items, string fieldName)
    {
        return items.OrderBy(item =>
        {
            var prop = item.GetType().GetProperty(fieldName);
            return prop?.GetValue(item)?.ToString() ?? string.Empty;
        }).ToList();
    }

    private static Dictionary<string, object?> ProjectFields(object item, List<string> fields)
    {
        var result = new Dictionary<string, object?>();
        var type = item.GetType();
        foreach (var field in fields)
        {
            var prop = type.GetProperty(field);
            result[field] = prop?.GetValue(item);
        }
        return result;
    }

    private static object BuildResult(List<object> items, string resultProperty)
    {
        var dict = new Dictionary<string, object>
        {
            [resultProperty] = items,
            ["TotalCount"] = items.Count
        };
        return dict;
    }

    private static string? GetStringParameter(Dictionary<string, object> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var value)) return null;
        return value?.ToString();
    }
}
