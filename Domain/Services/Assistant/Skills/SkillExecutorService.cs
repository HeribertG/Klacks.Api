// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Common;
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
    private readonly ISkillScenarioScopeGuard _scenarioScopeGuard;
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
        ISkillScenarioScopeGuard scenarioScopeGuard,
        ILogger<SkillExecutorService> logger)
    {
        _scenarioScopeGuard = scenarioScopeGuard;
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

    /// <summary>
    /// Runs the gates that may stop an otherwise valid invocation, in order: the scenario scope (an analyseToken of a
    /// scenario the caller cannot see is refused like an unknown one), then the autonomy gate. Returns the refusal
    /// (already tracked as a failure) or null when the skill may run.
    /// </summary>
    private async Task<SkillResult?> CheckPreExecutionGatesAsync(
        SkillDescriptor descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        TimeSpan elapsed,
        CancellationToken cancellationToken)
    {
        var scopeResult = await _scenarioScopeGuard.CheckAsync(invocation.Parameters, cancellationToken);
        if (scopeResult != null)
        {
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.ParameterInvalid, context, invocation.Parameters, scopeResult.Message, elapsed, descriptor.Category, cancellationToken);
            return scopeResult;
        }

        var gateResult = await _autonomyGate.CheckAsync(descriptor, context, invocation.Parameters, cancellationToken);
        if (gateResult != null)
        {
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.GateHold, context, invocation.Parameters, gateResult.Message, elapsed, descriptor.Category, cancellationToken);
            return gateResult;
        }

        return null;
    }

    public async Task<SkillResult> ExecuteAsync(
        SkillInvocation invocation,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        SkillDescriptor? descriptor = null;
        using var principal = BeginContextPrincipal(context);
        invocation = WithoutReservedParameters(invocation);
        context = await InstallationLanguageFallback.ApplyAsync(context, _serviceProvider, cancellationToken);

        try
        {
            descriptor = _registry.GetSkillByName(invocation.SkillName);
            if (descriptor == null)
            {
                return await RejectUnknownSkillAsync(invocation, context, stopwatch, cancellationToken);
            }

            var refusal = await CheckInvocationAsync(descriptor, invocation, context, stopwatch, cancellationToken);
            if (refusal != null)
            {
                return refusal;
            }

            var dispatch = await DispatchAsync(descriptor, invocation, context, stopwatch, cancellationToken);
            if (dispatch.IsRefusal)
            {
                return dispatch.Result;
            }

            return await CompleteSuccessfulRunAsync(descriptor, invocation, context, dispatch, stopwatch, cancellationToken);
        }
        catch (SkillException ex)
        {
            stopwatch.Stop();
            return await CompleteWithExceptionResultAsync(descriptor, invocation, context, SkillErrorResult(invocation.SkillName, ex), stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return await CompleteCancelledAsync(descriptor, invocation, context, stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is IUserFacingRejection)
        {
            stopwatch.Stop();
            return await CompleteWithExceptionResultAsync(descriptor, invocation, context, RejectionResult(invocation.SkillName, ex), stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return await CompleteWithExceptionResultAsync(descriptor, invocation, context, UnexpectedErrorResult(invocation.SkillName, ex), stopwatch.Elapsed);
        }
    }

    private async Task<SkillResult> RejectUnknownSkillAsync(
        SkillInvocation invocation,
        SkillExecutionContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Skill not found: {SkillName}", invocation.SkillName);
        var notFound = SkillResult.Error($"Skill '{invocation.SkillName}' not found");
        await TrackFailureAsync(invocation.SkillName, SkillFailureKind.NotFound, context, invocation.Parameters, notFound.Message, stopwatch.Elapsed, category: null, cancellationToken: cancellationToken);
        return notFound;
    }

    /// <summary>
    /// Runs the checks that may refuse a resolved skill, in order: permissions, parameters, the UI context of a UI
    /// action, an apiCall step in a UI action's stored config, then the pre-execution gates. Returns the refusal
    /// (already tracked as a failure) or null when the skill may be dispatched.
    /// </summary>
    private async Task<SkillResult?> CheckInvocationAsync(
        SkillDescriptor descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var permissionResult = ValidatePermissions(descriptor, context);
        if (!permissionResult.Success)
        {
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.PermissionDenied, context, invocation.Parameters, permissionResult.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
            return permissionResult;
        }

        var parameterResult = ValidateParameters(descriptor, invocation.Parameters, context.UserLanguage);
        if (!parameterResult.Success)
        {
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.ParameterInvalid, context, invocation.Parameters, parameterResult.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
            return parameterResult;
        }

        var isUiAction = IsUiAction(descriptor);
        if (isUiAction && !context.SupportsUiActions)
        {
            var uiContextError = SkillResult.Error(
                $"Skill '{invocation.SkillName}' requires an interactive UI session and cannot be executed in this context.");
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.UiActionContext, context, invocation.Parameters, uiContextError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
            return uiContextError;
        }

        if (isUiAction && UiActionStepInspector.ContainsApiCallStep(descriptor.HandlerConfig))
        {
            _logger.LogWarning(
                "UiAction skill {SkillName} carries an apiCall step in its stored handler config and is not delivered to the client",
                descriptor.Name);
            var apiCallError = SkillResult.Error(UiActionStepActions.ApiCallRejectedMessage);
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, apiCallError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
            return apiCallError;
        }

        return await CheckPreExecutionGatesAsync(descriptor, invocation, context, stopwatch.Elapsed, cancellationToken);
    }

    private static bool IsUiAction(SkillDescriptor descriptor) =>
        string.Equals(descriptor.ExecutionType, LlmExecutionTypes.UiAction, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Logs the execution and hands the invocation to its route: a UI action is returned to the browser, a generic handler type goes to the
    /// generic dispatcher, an implementation type is resolved from DI and run. A route that cannot run tracks the
    /// failure and returns it as a refusal, which skips the success post-processing.
    /// </summary>
    private async Task<SkillDispatchOutcome> DispatchAsync(
        SkillDescriptor descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing skill: {SkillName} for user {UserId}",
            descriptor.Name, context.UserId);

        if (IsUiAction(descriptor))
        {
            // W1.4: the tracking id is the usage-row id the frontend reports back under, so the
            // dispatch can later be resolved to a truthful Completed/Failed outcome.
            var uiActionTrackingId = Guid.NewGuid();
            var uiAction = SkillResult.UiAction(
                string.IsNullOrWhiteSpace(descriptor.HandlerConfig) ? EmptyUiActionSteps : descriptor.HandlerConfig,
                invocation.Parameters,
                $"Function '{descriptor.Name}' will be executed as UI action in the user's browser.",
                uiActionTrackingId);
            return SkillDispatchOutcome.Ran(uiAction, uiActionTrackingId);
        }

        if (_genericDispatcher.CanHandle(descriptor.HandlerType))
        {
            return await DispatchGenericHandlerAsync(descriptor, invocation, context, stopwatch, cancellationToken);
        }

        if (descriptor.ImplementationType != null)
        {
            return await DispatchImplementationAsync(descriptor, descriptor.ImplementationType, invocation, context, stopwatch, cancellationToken);
        }

        var noHandlerError = SkillResult.Error($"No implementation or handler found for skill '{invocation.SkillName}'.");
        await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, noHandlerError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
        return SkillDispatchOutcome.Refused(noHandlerError);
    }

    private async Task<SkillDispatchOutcome> DispatchGenericHandlerAsync(
        SkillDescriptor descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descriptor.HandlerConfig))
        {
            var noConfigError = SkillResult.Error($"Skill '{invocation.SkillName}' has HandlerType '{descriptor.HandlerType}' but no HandlerConfig.");
            await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, noConfigError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
            return SkillDispatchOutcome.Refused(noConfigError);
        }

        var result = await _genericDispatcher.ExecuteAsync(
            descriptor.HandlerType!,
            descriptor.HandlerConfig,
            invocation.Parameters,
            context.ExplainDepth, context.UserPermissions,
            cancellationToken);
        return SkillDispatchOutcome.Ran(result, null);
    }

    private async Task<SkillDispatchOutcome> DispatchImplementationAsync(
        SkillDescriptor descriptor,
        Type implementationType,
        SkillInvocation invocation,
        SkillExecutionContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var instance = _serviceProvider.GetRequiredService(implementationType);
        var skillParameters = WithExecutionContextKeys(invocation.Parameters, context);

        if (instance is ISkillImplementation impl)
        {
            return SkillDispatchOutcome.Ran(await impl.ExecuteAsync(context, skillParameters, cancellationToken), null);
        }

        if (instance is Klacks.Plugin.Contracts.Skills.ISkillImplementation pluginImpl)
        {
            return SkillDispatchOutcome.Ran(await ExecutePluginSkillAsync(pluginImpl, invocation, context, cancellationToken), null);
        }

        if (instance is ISkill skill)
        {
            return SkillDispatchOutcome.Ran(await skill.ExecuteAsync(context, skillParameters, cancellationToken), null);
        }

        var noImplError = SkillResult.Error($"Skill '{invocation.SkillName}' does not implement ISkillImplementation or ISkill.");
        await TrackFailureAsync(descriptor.Name, SkillFailureKind.Exception, context, invocation.Parameters, noImplError.Message, stopwatch.Elapsed, descriptor.Category, cancellationToken);
        return SkillDispatchOutcome.Refused(noImplError);
    }

    /// <summary>
    /// Writes the usage row of a dispatched skill (never cancelled, under the UI action's tracking id), logs the run,
    /// lets every non-UI result notify entity changes and register the recent entity, and taints the result of an
    /// untrusted-output skill.
    /// </summary>
    private async Task<SkillResult> CompleteSuccessfulRunAsync(
        SkillDescriptor descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        SkillDispatchOutcome dispatch,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var result = dispatch.Result;

        stopwatch.Stop();

        await _usageTracker.TrackAsync(
            descriptor,
            context,
            invocation.Parameters,
            result,
            stopwatch.Elapsed,
            CancellationToken.None,
            recordId: dispatch.UiActionTrackingId);

        _logger.LogInformation("Skill executed: {SkillName}, Success: {Success}, Duration: {Duration}ms",
            descriptor.Name, result.Success, stopwatch.ElapsedMilliseconds);

        if (result.UiActionSteps == null)
        {
            await NotifyEntityChangeAsync(descriptor, context, result, cancellationToken);

            await RegisterRecentEntityAsync(descriptor, context, result, cancellationToken);
        }

        return MarkExternalContent(descriptor.Name, result);
    }

    /// <summary>
    /// Answers a SkillException with its own message, error code (UNKNOWN when it has none) and the skill name it
    /// carries, logged as a warning under the invoked name.
    /// </summary>
    private SkillResult SkillErrorResult(string skillName, SkillException exception)
    {
        _logger.LogWarning(exception, "Skill error in {SkillName}: {ErrorCode} - {Message}",
            skillName, exception.ErrorCode, exception.Message);
        return SkillResult.Error(exception.Message, new Dictionary<string, object>
        {
            { SkillErrorKeys.ErrorCode, exception.ErrorCode ?? SkillErrorKeys.Unknown },
            { SkillErrorKeys.SkillName, exception.SkillName }
        });
    }

    /// <summary>
    /// Records a run the caller stopped as a failure of kind Cancelled, with a token no cancellation can reach.
    /// </summary>
    private async Task<SkillResult> CompleteCancelledAsync(
        SkillDescriptor? descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        TimeSpan elapsed)
    {
        _logger.LogInformation("Skill execution cancelled: {SkillName}", invocation.SkillName);
        var result = SkillResult.Cancelled($"Skill '{invocation.SkillName}' execution was cancelled");
        await TrackFailureAsync(descriptor?.Name ?? invocation.SkillName, SkillFailureKind.Cancelled, context, invocation.Parameters, result.Message, elapsed, descriptor?.Category, CancellationToken.None);
        return result;
    }

    /// <summary>
    /// Answers an IUserFacingRejection (sealed day, missing entity, conflict, missing right) with its own message, the
    /// same text ErrorHandlingMiddleware returns to an HTTP client, and logs it as an expected outcome, not as a fault.
    /// </summary>
    /// <param name="skillName">Skill name as invoked.</param>
    /// <param name="exception">The rejection thrown by the skill or the handler behind it.</param>
    private SkillResult RejectionResult(string skillName, Exception exception)
    {
        _logger.LogWarning("Skill {SkillName} rejected the request ({ExceptionType}): {Message}",
            skillName, exception.GetType().Name, exception.Message);
        return SkillResult.Error(exception.Message, new Dictionary<string, object>
        {
            { SkillErrorKeys.ErrorCode, SkillErrorKeys.Rejected },
            { SkillErrorKeys.SkillName, skillName }
        });
    }

    /// <summary>
    /// Answers an unexpected exception with an execution error and logs it as an error.
    /// </summary>
    /// <param name="skillName">Skill name as invoked.</param>
    /// <param name="exception">The unexpected exception.</param>
    private SkillResult UnexpectedErrorResult(string skillName, Exception exception)
    {
        _logger.LogError(exception, "Unexpected error executing skill: {SkillName}", skillName);
        return SkillResult.Error($"Execution error: {exception.Message}", new Dictionary<string, object>
        {
            { SkillErrorKeys.ErrorCode, SkillErrorKeys.ExecutionError },
            { SkillErrorKeys.SkillName, skillName },
            { SkillErrorKeys.ExceptionType, exception.GetType().Name }
        });
    }

    /// <summary>
    /// Records an execution that ended in an exception as a failure of kind Exception and returns the error result,
    /// tainted like any other result of an untrusted-output skill.
    /// </summary>
    /// <param name="descriptor">Resolved skill, or null when the exception came before the registry lookup.</param>
    /// <param name="invocation">Invocation as executed.</param>
    /// <param name="context">Execution context of the caller.</param>
    /// <param name="result">Error result built from the exception.</param>
    /// <param name="elapsed">Time spent until the exception.</param>
    private async Task<SkillResult> CompleteWithExceptionResultAsync(
        SkillDescriptor? descriptor,
        SkillInvocation invocation,
        SkillExecutionContext context,
        SkillResult result,
        TimeSpan elapsed)
    {
        var skillName = descriptor?.Name ?? invocation.SkillName;
        await TrackFailureAsync(skillName, SkillFailureKind.Exception, context, invocation.Parameters, result.Message, elapsed, descriptor?.Category, CancellationToken.None);
        return MarkExternalContent(skillName, result);
    }

    /// <summary>
    /// Runs a plugin skill with the plugin contract's reduced context and maps its result back.
    /// </summary>
    private static async Task<SkillResult> ExecutePluginSkillAsync(
        Klacks.Plugin.Contracts.Skills.ISkillImplementation pluginImpl,
        SkillInvocation invocation,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var pluginContext = new Klacks.Plugin.Contracts.Skills.SkillExecutionContext
        {
            UserId = context.UserId,
            TenantId = context.TenantId,
            UserName = context.UserName,
            UserPermissions = context.UserPermissions,
            ConfirmedBinding = context.ConfirmedActionBinding is { } binding
                ? new Klacks.Plugin.Contracts.Skills.PluginConfirmedBinding(binding.Kind, binding.Items)
                : null
        };
        var pluginResult = await pluginImpl.ExecuteAsync(pluginContext, invocation.Parameters, cancellationToken);
        return MapPluginSkillResult(pluginResult);
    }

    /// <summary>
    /// Runs the invocation (gates included) on behalf of the context's user when the async flow has no owner yet.
    /// Every visibility guard reads the ambient user (HTTP claim, else ExecutionPrincipal); skills reached outside a
    /// request (Telegram, inbound automation, proactive remediation) know their acting user only from the context and
    /// would otherwise be checked as an unrestricted system job. An owner set further up (plans, scheduled tasks) wins.
    /// </summary>
    /// <param name="context">Execution context whose UserId becomes the ambient owner.</param>
    private static IDisposable? BeginContextPrincipal(SkillExecutionContext context)
    {
        if (ExecutionPrincipal.CurrentUserId != null || context.UserId == Guid.Empty)
        {
            return null;
        }

        return ExecutionPrincipal.Begin(context.UserId);
    }

    /// <summary>
    /// Drops the parameter keys that could carry the stale-scenario consent (ScenarioAcceptDefaults.ReservedParameters)
    /// before anything else sees the invocation - gates, the autonomy hold that stores arguments verbatim, the skill.
    /// That consent exists only as SkillExecutionContext.StaleScenarioOverwriteAcknowledgement, so no model and no
    /// background caller can supply it as an argument.
    /// </summary>
    /// <param name="invocation">Invocation exactly as the caller sent it.</param>
    private static SkillInvocation WithoutReservedParameters(SkillInvocation invocation)
    {
        if (!ScenarioAcceptDefaults.ReservedParameters.Any(invocation.Parameters.ContainsKey))
        {
            return invocation;
        }

        var parameters = new Dictionary<string, object>(invocation.Parameters);
        foreach (var reserved in ScenarioAcceptDefaults.ReservedParameters)
        {
            parameters.Remove(reserved);
        }

        return invocation with { Parameters = parameters };
    }

    /// <summary>
    /// Taints the result of a skill listed in UntrustedSkillOutputs so its external content stays framed as
    /// untrusted even when a wrapper skill relays it under its own name. An existing taint is never cleared,
    /// because a wrapper (confirm_pending_action) returns the already tainted result of the inner skill.
    /// </summary>
    /// <param name="skillName">Canonical name of the skill that produced the result.</param>
    /// <param name="result">Result as returned by the skill.</param>
    private static SkillResult MarkExternalContent(string skillName, SkillResult result)
    {
        if (result.ContainsExternalContent || !UntrustedSkillOutputs.Contains(skillName))
        {
            return result;
        }

        return result with { ContainsExternalContent = true };
    }

    /// <summary>
    /// Copy of the invocation arguments carrying the reserved execution-context keys a skill's
    /// parameter reader needs but no skill declares - today the caller's UI language, so an ambiguous
    /// date like "03/04/2026" is read the way that user writes it. Only in-process skills get this
    /// copy: validation, usage tracking, UI actions, plugin skills and the generic dispatcher keep the
    /// original dictionary, so the reserved key never reaches a log, the frontend or an outgoing call.
    /// </summary>
    /// <param name="parameters">Invocation arguments exactly as the LLM sent them.</param>
    /// <param name="context">Execution context the reserved values are taken from.</param>
    private static Dictionary<string, object> WithExecutionContextKeys(
        Dictionary<string, object> parameters,
        SkillExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.UserLanguage))
        {
            return parameters;
        }

        return new Dictionary<string, object>(parameters)
        {
            [SkillParameterKeys.UserLanguage] = context.UserLanguage
        };
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

    /// <summary>
    /// Refuses a skill of CappedChannelSkillExclusions on a capped channel (Slack owner bridge, MCP) whatever the rights say, then
    /// checks the rights the skill requires and, for the skills of GroupVisibilityBoundSkills, that somebody is identified:
    /// the visibility guards read the ambient user and answer "unrestricted" for no user at all, which is right for a
    /// background job but would hand a call made for a person every client's data. After BeginContextPrincipal the ambient
    /// owner is set exactly when the context names a user or an owner further up already runs the flow; an HTTP claim is
    /// not consulted, so a request whose context lost its user id is refused too. Admins are unrestricted either way.
    /// </summary>
    /// <param name="descriptor">Skill about to run.</param>
    /// <param name="context">Execution context of the caller.</param>
    private static SkillResult ValidatePermissions(SkillDescriptor descriptor, SkillExecutionContext context)
    {
        if (CappedChannelSkillExclusions.Blocks(descriptor.Name, context))
        {
            return SkillResult.Error(CappedChannelSkillExclusions.DescribeNotAvailable(descriptor.Name));
        }

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

        if (GroupVisibilityBoundSkills.Contains(descriptor.Name) && ExecutionPrincipal.CurrentUserId == null)
        {
            return SkillResult.Error(GroupVisibilityBoundSkills.UnidentifiedCallerMessage);
        }

        return SkillResult.SuccessResult(null);
    }

    private static SkillResult ValidateParameters(
        SkillDescriptor descriptor,
        Dictionary<string, object> parameters,
        string? language)
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

        var typeErrors = SkillParameterTypeValidator.Validate(descriptor, parameters, language);
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

    /// <summary>
    /// What a dispatch route produced: either a result for the success post-processing, or a refusal that was
    /// already tracked as a failure and is returned as it is.
    /// </summary>
    /// <param name="Result">The skill result or the refusal.</param>
    /// <param name="IsRefusal">True when the route could not run the skill.</param>
    /// <param name="UiActionTrackingId">Usage-row id of a UI action, null for every other route.</param>
    private sealed record SkillDispatchOutcome(SkillResult Result, bool IsRefusal, Guid? UiActionTrackingId)
    {
        public static SkillDispatchOutcome Ran(SkillResult result, Guid? uiActionTrackingId) => new(result, false, uiActionTrackingId);

        public static SkillDispatchOutcome Refused(SkillResult refusal) => new(refusal, true, null);
    }
}
