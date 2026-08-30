// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillExecutorService : ISkillExecutor
{
    private const string EmptyUiActionSteps = "{}";

    private readonly ISkillRegistry _registry;
    private readonly ISkillUsageTracker _usageTracker;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGenericSkillDispatcher _genericDispatcher;
    private readonly IAutonomyGate _autonomyGate;
    private readonly IEntityChangeNotifier _entityChangeNotifier;
    private readonly IRecentEntityRegistrar _recentEntityRegistrar;
    private readonly ILogger<SkillExecutorService> _logger;

    public SkillExecutorService(
        ISkillRegistry registry,
        ISkillUsageTracker usageTracker,
        IServiceProvider serviceProvider,
        IGenericSkillDispatcher genericDispatcher,
        IAutonomyGate autonomyGate,
        IEntityChangeNotifier entityChangeNotifier,
        IRecentEntityRegistrar recentEntityRegistrar,
        ILogger<SkillExecutorService> logger)
    {
        _registry = registry;
        _usageTracker = usageTracker;
        _serviceProvider = serviceProvider;
        _genericDispatcher = genericDispatcher;
        _autonomyGate = autonomyGate;
        _entityChangeNotifier = entityChangeNotifier;
        _recentEntityRegistrar = recentEntityRegistrar;
        _logger = logger;
    }

    private static SkillResult MapPluginSkillResult(Klacks.Plugin.Contracts.Skills.SkillResult pluginResult)
    {
        return new SkillResult
        {
            Success = pluginResult.Success,
            Data = pluginResult.Data,
            Message = pluginResult.Message,
            Type = pluginResult.Type switch
            {
                Klacks.Plugin.Contracts.Skills.SkillResultType.Data => SkillResultType.Data,
                Klacks.Plugin.Contracts.Skills.SkillResultType.Error => SkillResultType.Error,
                Klacks.Plugin.Contracts.Skills.SkillResultType.Navigation => SkillResultType.Navigation,
                Klacks.Plugin.Contracts.Skills.SkillResultType.Cancelled => SkillResultType.Cancelled,
                Klacks.Plugin.Contracts.Skills.SkillResultType.Confirmation => SkillResultType.Confirmation,
                _ => SkillResultType.Data
            }
        };
    }

    public async Task<SkillResult> ExecuteAsync(
        SkillInvocation invocation,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        SkillDescriptor? descriptor = null;

        try
        {
            descriptor = _registry.GetSkillByName(invocation.SkillName);
            if (descriptor == null)
            {
                _logger.LogWarning("Skill not found: {SkillName}", invocation.SkillName);
                var notFound = SkillResult.Error($"Skill '{invocation.SkillName}' not found");
                await TrackFailureAsync(invocation.SkillName, SkillFailureKind.NotFound, context, invocation.Parameters, notFound.Message, stopwatch.Elapsed, category: null, cancellationToken: cancellationToken);
                return notFound;
            }

            var permissionResult = ValidatePermissions(descriptor, context);
            if (!permissionResult.Success)
            {
                await TrackFailureAsync(descriptor.Name, SkillFailureKind.PermissionDenied, context, invocation.Parameters, permissionResult.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                return permissionResult;
            }

            var parameterResult = ValidateParameters(descriptor, invocation.Parameters);
            if (!parameterResult.Success)
            {
                await TrackFailureAsync(descriptor.Name, SkillFailureKind.ParameterInvalid, context, invocation.Parameters, parameterResult.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                return parameterResult;
            }

            var isUiAction = string.Equals(descriptor.ExecutionType, LlmExecutionTypes.UiAction, StringComparison.OrdinalIgnoreCase);
            if (isUiAction && !context.SupportsUiActions)
            {
                var uiContextError = SkillResult.Error(
                    $"Skill '{invocation.SkillName}' requires an interactive UI session and cannot be executed in this context.");
                await TrackFailureAsync(descriptor.Name, SkillFailureKind.UiActionContext, context, invocation.Parameters, uiContextError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                return uiContextError;
            }

            var gateResult = await _autonomyGate.CheckAsync(descriptor, context, invocation.Parameters, cancellationToken);
            if (gateResult != null)
            {
                await TrackFailureAsync(descriptor.Name, SkillFailureKind.GateHold, context, invocation.Parameters, gateResult.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                return gateResult;
            }

            _logger.LogInformation("Executing skill: {SkillName} for user {UserId}",
                descriptor.Name, context.UserId);

            SkillResult result;
            Guid? uiActionTrackingId = null;

            if (isUiAction)
            {
                // W1.4: the tracking id is the usage-row id the frontend reports back under, so the
                // dispatch can later be resolved to a truthful Completed/Failed outcome.
                uiActionTrackingId = Guid.NewGuid();
                result = SkillResult.UiAction(
                    string.IsNullOrWhiteSpace(descriptor.HandlerConfig) ? EmptyUiActionSteps : descriptor.HandlerConfig,
                    invocation.Parameters,
                    $"Function '{descriptor.Name}' will be executed as UI action in the user's browser.",
                    uiActionTrackingId);
            }
            else if (_genericDispatcher.CanHandle(descriptor.HandlerType))
            {
                if (string.IsNullOrWhiteSpace(descriptor.HandlerConfig))
                {
                    var noConfigError = SkillResult.Error($"Skill '{invocation.SkillName}' has HandlerType '{descriptor.HandlerType}' but no HandlerConfig.");
                    await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, noConfigError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                    return noConfigError;
                }

                result = await _genericDispatcher.ExecuteAsync(
                    descriptor.HandlerType!,
                    descriptor.HandlerConfig,
                    invocation.Parameters,
                    cancellationToken);
            }
            else if (descriptor.ImplementationType != null)
            {
                var instance = _serviceProvider.GetRequiredService(descriptor.ImplementationType);

                if (instance is ISkillImplementation impl)
                {
                    result = await impl.ExecuteAsync(context, invocation.Parameters, cancellationToken);
                }
                else if (instance is Klacks.Plugin.Contracts.Skills.ISkillImplementation pluginImpl)
                {
                    var pluginContext = new Klacks.Plugin.Contracts.Skills.SkillExecutionContext
                    {
                        UserId = context.UserId,
                        TenantId = context.TenantId,
                        UserName = context.UserName,
                        UserPermissions = context.UserPermissions
                    };
                    var pluginResult = await pluginImpl.ExecuteAsync(pluginContext, invocation.Parameters, cancellationToken);
                    result = MapPluginSkillResult(pluginResult);
                }
                else if (instance is ISkill skill)
                {
                    result = await skill.ExecuteAsync(context, invocation.Parameters, cancellationToken);
                }
                else
                {
                    var noImplError = SkillResult.Error($"Skill '{invocation.SkillName}' does not implement ISkillImplementation or ISkill.");
                    await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, noImplError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                    return noImplError;
                }
            }
            else
            {
                var noHandlerError = SkillResult.Error($"No implementation or handler found for skill '{invocation.SkillName}'.");
                await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, noHandlerError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
                return noHandlerError;
            }

            stopwatch.Stop();

            await _usageTracker.TrackAsync(
                descriptor,
                context,
                invocation.Parameters,
                result,
                stopwatch.Elapsed,
                cancellationToken,
                recordId: uiActionTrackingId);

            _logger.LogInformation("Skill executed: {SkillName}, Success: {Success}, Duration: {Duration}ms",
                descriptor.Name, result.Success, stopwatch.ElapsedMilliseconds);

            if (result.UiActionSteps == null)
            {
                await NotifyEntityChangeAsync(descriptor, context, result, cancellationToken);

                await RegisterRecentEntityAsync(descriptor, context, result, cancellationToken);
            }

            return result;
        }
        catch (SkillException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Skill error in {SkillName}: {ErrorCode} - {Message}",
                invocation.SkillName, ex.ErrorCode, ex.Message);
            var result = SkillResult.Error(ex.Message, new Dictionary<string, object>
            {
                { SkillErrorKeys.ErrorCode, ex.ErrorCode ?? SkillErrorKeys.Unknown },
                { SkillErrorKeys.SkillName, ex.SkillName }
            });
            await TrackFailureAsync(descriptor?.Name ?? invocation.SkillName, SkillFailureKind.Exception, context, invocation.Parameters, result.Message, stopwatch.Elapsed, descriptor?.Category, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation("Skill execution cancelled: {SkillName}", invocation.SkillName);
            var result = SkillResult.Cancelled($"Skill '{invocation.SkillName}' execution was cancelled");
            await TrackFailureAsync(descriptor?.Name ?? invocation.SkillName, SkillFailureKind.Exception, context, invocation.Parameters, result.Message, stopwatch.Elapsed, descriptor?.Category, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing skill: {SkillName}", invocation.SkillName);
            var result = SkillResult.Error($"Execution error: {ex.Message}", new Dictionary<string, object>
            {
                { SkillErrorKeys.ErrorCode, SkillErrorKeys.ExecutionError },
                { SkillErrorKeys.SkillName, invocation.SkillName },
                { SkillErrorKeys.ExceptionType, ex.GetType().Name }
            });
            await TrackFailureAsync(descriptor?.Name ?? invocation.SkillName, SkillFailureKind.Exception, context, invocation.Parameters, result.Message, stopwatch.Elapsed, descriptor?.Category, cancellationToken);
            return result;
        }
    }

    private async Task TrackFailureAsync(
        string skillName,
        SkillFailureKind failureKind,
        SkillExecutionContext context,
        Dictionary<string, object>? parameters,
        string? errorMessage,
        TimeSpan duration,
        SkillCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _usageTracker.TrackFailureAsync(
                skillName,
                failureKind,
                context,
                parameters,
                errorMessage,
                duration,
                category ?? SkillCategory.Action,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failure tracking failed for {SkillName} ({FailureKind})", skillName, failureKind);
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
        if (context.UserPermissions.Contains(Roles.Admin))
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

        var typeErrors = SkillParameterTypeValidator.Validate(descriptor, parameters);
        if (typeErrors.Count > 0)
        {
            return SkillResult.Error(
                $"Invalid parameter values: {string.Join(" ", typeErrors)}");
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

    private async Task NotifyEntityChangeAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        SkillResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _entityChangeNotifier.NotifyExecutedAsync(descriptor, context, result, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entity-changed notification failed for skill {SkillName}", descriptor.Name);
        }
    }

    private async Task RegisterRecentEntityAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        SkillResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _recentEntityRegistrar.RegisterAsync(descriptor, context, result, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recent-entity registration failed for skill {SkillName}", descriptor.Name);
        }
    }
}
