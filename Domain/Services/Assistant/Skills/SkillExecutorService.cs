using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillExecutorService : ISkillExecutor
{
    private readonly ISkillRegistry _registry;
    private readonly ISkillUsageTracker _usageTracker;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkillExecutorService> _logger;

    public SkillExecutorService(
        ISkillRegistry registry,
        ISkillUsageTracker usageTracker,
        IServiceProvider serviceProvider,
        ILogger<SkillExecutorService> logger)
    {
        _registry = registry;
        _usageTracker = usageTracker;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<SkillResult> ExecuteAsync(
        SkillInvocation invocation,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var descriptor = _registry.GetSkillByName(invocation.SkillName);
            if (descriptor == null)
            {
                _logger.LogWarning("Skill not found: {SkillName}", invocation.SkillName);
                return SkillResult.Error($"Skill '{invocation.SkillName}' not found");
            }

            var permissionResult = ValidatePermissions(descriptor, context);
            if (!permissionResult.Success)
            {
                return permissionResult;
            }

            var parameterResult = ValidateParameters(descriptor, invocation.Parameters);
            if (!parameterResult.Success)
            {
                return parameterResult;
            }

            _logger.LogInformation("Executing skill: {SkillName} for user {UserId}",
                descriptor.Name, context.UserId);

            var skill = (ISkill)_serviceProvider.GetRequiredService(descriptor.ImplementationType);
            var result = await skill.ExecuteAsync(context, invocation.Parameters, cancellationToken);

            stopwatch.Stop();

            await _usageTracker.TrackAsync(
                descriptor,
                context,
                invocation.Parameters,
                result,
                stopwatch.Elapsed,
                cancellationToken);

            _logger.LogInformation("Skill executed: {SkillName}, Success: {Success}, Duration: {Duration}ms",
                descriptor.Name, result.Success, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (SkillException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Skill error in {SkillName}: {ErrorCode} - {Message}",
                invocation.SkillName, ex.ErrorCode, ex.Message);
            return SkillResult.Error(ex.Message, new Dictionary<string, object>
            {
                { "errorCode", ex.ErrorCode ?? "UNKNOWN" },
                { "skillName", ex.SkillName }
            });
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation("Skill execution cancelled: {SkillName}", invocation.SkillName);
            return SkillResult.Cancelled($"Skill '{invocation.SkillName}' execution was cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing skill: {SkillName}", invocation.SkillName);
            return SkillResult.Error($"Execution error: {ex.Message}", new Dictionary<string, object>
            {
                { "errorCode", "EXECUTION_ERROR" },
                { "skillName", invocation.SkillName },
                { "exceptionType", ex.GetType().Name }
            });
        }
    }

    public async Task<IReadOnlyList<SkillResult>> ExecuteChainAsync(
        IReadOnlyList<SkillInvocation> invocations,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SkillResult>();
        var sharedContext = new Dictionary<string, object>();

        for (var i = 0; i < invocations.Count; i++)
        {
            var invocation = invocations[i];

            var enrichedParams = EnrichWithPreviousResults(invocation.Parameters, sharedContext);
            var enrichedInvocation = invocation with { Parameters = enrichedParams };

            var result = await ExecuteAsync(enrichedInvocation, context, cancellationToken);
            results.Add(result);

            sharedContext[$"step_{i + 1}"] = result.Data ?? new object();
            sharedContext[$"result_{invocation.SkillName}"] = result.Data ?? new object();

            if (!result.Success && invocation.StopOnError)
            {
                _logger.LogWarning("Chain execution stopped at step {Step} due to error: {Message}",
                    i + 1, result.Message);
                break;
            }
        }

        return results;
    }

    private static SkillResult ValidatePermissions(SkillDescriptor descriptor, SkillExecutionContext context)
    {
        if (context.UserPermissions.Contains("Admin"))
        {
            return SkillResult.SuccessResult(null);
        }

        var missingPermissions = descriptor.RequiredPermissions
            .Where(rp => !context.UserPermissions.Contains(rp))
            .ToList();

        if (missingPermissions.Count > 0)
        {
            return SkillResult.Error(
                $"Permission denied. Missing permissions: {string.Join(", ", missingPermissions)}");
        }

        return SkillResult.SuccessResult(null);
    }

    private static SkillResult ValidateParameters(SkillDescriptor descriptor, Dictionary<string, object> parameters)
    {
        var missingRequired = descriptor.Parameters
            .Where(p => p.Required && !parameters.ContainsKey(p.Name))
            .Select(p => p.Name)
            .ToList();

        if (missingRequired.Count > 0)
        {
            return SkillResult.Error(
                $"Missing required parameters: {string.Join(", ", missingRequired)}");
        }

        return SkillResult.SuccessResult(null);
    }

    private static Dictionary<string, object> EnrichWithPreviousResults(
        Dictionary<string, object> parameters,
        Dictionary<string, object> sharedContext)
    {
        var enriched = new Dictionary<string, object>(parameters);
        var placeholderPattern = new Regex(@"\{\{([^}]+)\}\}");

        foreach (var (key, value) in parameters)
        {
            if (value is not string strValue) continue;

            var match = placeholderPattern.Match(strValue);
            if (!match.Success) continue;

            var path = match.Groups[1].Value;
            var resolved = ResolvePath(sharedContext, path);
            if (resolved != null)
            {
                enriched[key] = resolved;
            }
        }

        return enriched;
    }

    private static object? ResolvePath(Dictionary<string, object> context, string path)
    {
        var parts = path.Split('.');
        object? current = context;

        foreach (var part in parts)
        {
            if (current == null) return null;

            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var property = current.GetType().GetProperty(part);
                if (property == null) return null;
                current = property.GetValue(current);
            }
        }

        return current;
    }
}
