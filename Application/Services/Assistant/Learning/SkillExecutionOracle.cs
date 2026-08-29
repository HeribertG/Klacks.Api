// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Oracle O2. Asks two questions about a composed capability and refuses to confuse them: is it allowed
/// to exist, and does it work.
/// The first half is static and applies to every step. A step whose skill the registry does not know, or
/// that the risk classifier does not place in ReadOnly or Reversible, rejects the whole composition -
/// fail-closed, and deliberately without asking why the classifier said what it said: the classifier
/// returns Sensitive both for a skill on its sensitive list and for one it simply cannot place, and a
/// composition built on a skill nobody can rate is not one to activate either way.
/// The second half runs only what reads. Klacks has no rollback and no test tenant, so a Reversible step
/// is checked and never executed; a capability containing one is activated on its static proof alone and
/// owes its first real, user-confirmed run as the missing evidence. Execution stops at the first step
/// that reads a slot rather than a constant, because slot values are produced by the previous step at
/// runtime and inventing them would test the composition against data that does not exist.
/// The identity is the wishing user's own, with the autonomy gate left switched on: a step that would
/// stop and ask a human during a real turn must stop and ask here too, otherwise the probe would prove
/// something the user will never experience.
/// </summary>
/// <param name="registry">Resolves a step's skill name to its descriptor</param>
/// <param name="riskClassifier">Rates each step's skill</param>
/// <param name="identityProvider">Mints the short-lived identity the read-only steps run under</param>
/// <param name="skillExecutor">Runs the read-only steps through the production execution path</param>
/// <param name="logger">Reports probes that could not be judged</param>

using System.Diagnostics;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillExecutionOracle : ISkillExecutionOracle
{
    private static readonly IReadOnlyList<SkillRiskClass> AllowedRiskClasses =
        [SkillRiskClass.ReadOnly, SkillRiskClass.Reversible];

    private readonly ISkillRegistry _registry;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly IProactiveActionIdentityProvider _identityProvider;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ILogger<SkillExecutionOracle> _logger;

    public SkillExecutionOracle(
        ISkillRegistry registry,
        ISkillRiskClassifier riskClassifier,
        IProactiveActionIdentityProvider identityProvider,
        ISkillExecutor skillExecutor,
        ILogger<SkillExecutionOracle> logger)
    {
        _registry = registry;
        _riskClassifier = riskClassifier;
        _identityProvider = identityProvider;
        _skillExecutor = skillExecutor;
        _logger = logger;
    }

    public async Task<SkillExecutionProbe> ProbeAsync(
        IReadOnlyList<RecipeStep> steps,
        string? ownerUserId,
        Guid probeId,
        CancellationToken cancellationToken = default)
    {
        var plan = BuildStaticPlan(steps);
        if (plan.Error != null)
        {
            return SkillExecutionProbe.Rejected(plan.Error, []);
        }

        var executable = plan.Steps.TakeWhile(IsExecutable).ToList();
        if (executable.Count == 0)
        {
            return SkillExecutionProbe.Passed(false, [.. plan.Steps.Select(ToUnexecutedProbe)]);
        }

        var identity = await _identityProvider.ResolveForSkillAsync(
            ParseOwner(ownerUserId), probeId, executable[0].Skill, cancellationToken);

        if (!identity.Success || identity.Context == null)
        {
            _logger.LogInformation(
                "Execution probe {ProbeId} could not run: {Reason}", probeId, identity.Reason);

            return SkillExecutionProbe.Inconclusive(
                identity.Reason ?? "No identity was available to run the read-only steps under.");
        }

        // The provider mints its identity for the unattended action path, which bypasses the autonomy
        // gate and cannot answer a UI action. A probe must experience neither: it has to meet the same
        // gate a real turn meets, and a step that wants the browser is already rejected statically.
        var context = identity.Context with { BypassAutonomyGate = false, SupportsUiActions = false };

        return await ExecuteAsync(plan.Steps, executable.Count, context, cancellationToken);
    }

    private async Task<SkillExecutionProbe> ExecuteAsync(
        IReadOnlyList<StaticStep> steps,
        int executableCount,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<SkillExecutionStepProbe>();

        for (var index = 0; index < steps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = steps[index];
            if (index >= executableCount)
            {
                results.Add(ToUnexecutedProbe(step));
                continue;
            }

            var outcome = await RunStepAsync(step, context, cancellationToken);
            results.Add(outcome);

            if (!outcome.Success)
            {
                return SkillExecutionProbe.Rejected(
                    $"Step {index + 1} ('{step.Skill}') did not work: {outcome.Error}", results);
            }
        }

        return SkillExecutionProbe.Passed(executableCount == steps.Count, results);
    }

    private async Task<SkillExecutionStepProbe> RunStepAsync(
        StaticStep step, SkillExecutionContext context, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _skillExecutor.ExecuteAsync(
                new SkillInvocation { SkillName = step.Skill, Parameters = step.Constants },
                context,
                cancellationToken);

            stopwatch.Stop();

            // A confirmation is not a failure of the skill but it is a failure of the probe: nobody is
            // there to confirm, so the composition was never shown to run end to end.
            if (result.Type == SkillResultType.Confirmation)
            {
                return new SkillExecutionStepProbe(
                    step.Skill, step.RiskClass.ToString(), true, false, stopwatch.ElapsedMilliseconds,
                    "The step asked for a confirmation, which an unattended probe cannot give.");
            }

            return new SkillExecutionStepProbe(
                step.Skill,
                step.RiskClass.ToString(),
                true,
                result.Success,
                stopwatch.ElapsedMilliseconds,
                result.Success ? null : result.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();

            return new SkillExecutionStepProbe(
                step.Skill, step.RiskClass.ToString(), true, false, stopwatch.ElapsedMilliseconds,
                exception.Message);
        }
    }

    // Read-only and fed entirely by constants: those are the two conditions under which running a step
    // proves something. The moment either fails, the remaining steps are only checked, never run.
    private static bool IsExecutable(StaticStep step) =>
        step.RiskClass == SkillRiskClass.ReadOnly && !step.HasSlotReferences;

    private static SkillExecutionStepProbe ToUnexecutedProbe(StaticStep step) =>
        new(step.Skill, step.RiskClass.ToString(), false, false, 0,
            step.RiskClass == SkillRiskClass.ReadOnly
                ? "Not run: the step takes a value the previous step produces at runtime."
                : "Not run: the step writes, and there is no rollback to run it under.");

    private StaticPlan BuildStaticPlan(IReadOnlyList<RecipeStep> steps)
    {
        if (steps.Count == 0)
        {
            return new StaticPlan([], "A capability needs at least one step.");
        }

        if (steps.Count > SkillLearningDefaults.MaxCapabilityStepCount)
        {
            return new StaticPlan(
                [],
                $"A learned capability may have at most {SkillLearningDefaults.MaxCapabilityStepCount} steps.");
        }

        var captured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var checkedSteps = new List<StaticStep>();

        foreach (var step in steps)
        {
            var (checkedStep, error) = CheckStep(step, captured);
            if (error != null)
            {
                return new StaticPlan([], error);
            }

            checkedSteps.Add(checkedStep!);
            RegisterCapture(step, captured);
        }

        return new StaticPlan(checkedSteps, null);
    }

    private (StaticStep? Step, string? Error) CheckStep(RecipeStep step, IReadOnlySet<string> captured)
    {
        // Question steps are excluded from learned capabilities altogether: their answers would have to
        // be invented for the probe, and a composition proved against invented data is not proved.
        if (!string.Equals(step.Kind, RecipeStepKinds.Search, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(step.Kind, RecipeStepKinds.Mutate, StringComparison.OrdinalIgnoreCase))
        {
            return (null, $"Step kind '{step.Kind}' is not allowed in a learned capability.");
        }

        if (string.IsNullOrWhiteSpace(step.Skill))
        {
            return (null, "Every step of a learned capability must name a skill.");
        }

        var descriptor = _registry.GetSkillByName(step.Skill);
        if (descriptor == null)
        {
            return (null, $"Skill '{step.Skill}' does not exist.");
        }

        if (string.Equals(descriptor.ExecutionType, LlmExecutionTypes.UiAction, StringComparison.OrdinalIgnoreCase))
        {
            return (null, $"Skill '{step.Skill}' is a browser action and cannot run inside a capability.");
        }

        var riskClass = _riskClassifier.Classify(descriptor);
        if (!AllowedRiskClasses.Contains(riskClass))
        {
            return (null, $"Skill '{step.Skill}' is classified as {riskClass} and may not be composed.");
        }

        if (!string.IsNullOrWhiteSpace(step.Capture)
            && !string.Equals(step.Kind, RecipeStepKinds.Search, StringComparison.OrdinalIgnoreCase))
        {
            return (null, $"Only a search step may capture a value, but '{step.Skill}' is a {step.Kind} step.");
        }

        var bindingError = CheckParameterBinding(step, descriptor, captured);
        if (bindingError != null)
        {
            return (null, bindingError);
        }

        return (new StaticStep(step.Skill!, riskClass, HasSlotReferences(step), Constants(step)), null);
    }

    private static string? CheckParameterBinding(
        RecipeStep step, SkillDescriptor descriptor, IReadOnlySet<string> captured)
    {
        var inject = step.Inject ?? [];

        foreach (var reference in inject.Values)
        {
            var slot = SlotNameOf(reference);
            if (slot != null && !captured.Contains(slot))
            {
                return $"Step '{step.Skill}' reads '{reference}', which no earlier step produces.";
            }

            if (slot == null && string.IsNullOrWhiteSpace(reference))
            {
                return $"Step '{step.Skill}' binds a parameter to an empty value.";
            }
        }

        foreach (var parameter in descriptor.Parameters.Where(p => p.Required))
        {
            if (!inject.ContainsKey(parameter.Name) && parameter.DefaultValue == null)
            {
                return $"Step '{step.Skill}' leaves the required parameter '{parameter.Name}' unbound.";
            }
        }

        return null;
    }

    private static void RegisterCapture(RecipeStep step, HashSet<string> captured)
    {
        var slot = CaptureSlotOf(step.Capture);
        if (slot != null)
        {
            captured.Add(slot);
        }
    }

    private static string? CaptureSlotOf(string? capture)
    {
        if (string.IsNullOrWhiteSpace(capture))
        {
            return null;
        }

        var separator = capture.IndexOf(RecipeEngineDefaults.CaptureSeparator, StringComparison.OrdinalIgnoreCase);
        return separator < 0
            ? null
            : capture[(separator + RecipeEngineDefaults.CaptureSeparator.Length)..].Trim();
    }

    private static string? SlotNameOf(string? reference) =>
        !string.IsNullOrEmpty(reference)
        && reference.StartsWith(RecipeEngineDefaults.SlotReferencePrefix, StringComparison.Ordinal)
            ? reference[RecipeEngineDefaults.SlotReferencePrefix.Length..]
            : null;

    private static bool HasSlotReferences(RecipeStep step) =>
        step.Inject != null && step.Inject.Values.Any(value => SlotNameOf(value) != null);

    private static Dictionary<string, object> Constants(RecipeStep step)
    {
        var constants = new Dictionary<string, object>(StringComparer.Ordinal);
        if (step.Inject == null)
        {
            return constants;
        }

        foreach (var (parameter, reference) in step.Inject)
        {
            if (SlotNameOf(reference) == null && !string.IsNullOrWhiteSpace(reference))
            {
                constants[parameter] = reference;
            }
        }

        return constants;
    }

    private static Guid? ParseOwner(string? ownerUserId) =>
        Guid.TryParse(ownerUserId, out var parsed) ? parsed : null;

    private sealed record StaticStep(
        string Skill, SkillRiskClass RiskClass, bool HasSlotReferences, Dictionary<string, object> Constants);

    private sealed record StaticPlan(IReadOnlyList<StaticStep> Steps, string? Error);
}
