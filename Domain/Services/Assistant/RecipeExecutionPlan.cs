// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A data-driven recipe in flight: an ordered list of steps with a slot bag carried between them.
/// Two step directions: "pull" (ask) emits a question and stops the turn so the slot bag persists;
/// "push" (search/mutate) forces a skill, injects slot values ($-prefixed references) or literal
/// constants (values without the $ prefix) into its parameters, captures a value
/// from its result into a slot, and advances within the same turn. A step is skipped when its slot
/// is already filled — ask whose slot is set, or search whose capture target is set — which is how
/// "don't re-ask what was already said" and the GUID fast-path fall out of one rule.
/// </summary>
/// <param name="name">The recipe name.</param>
/// <param name="steps">The ordered steps (deserialized from the recipe definition).</param>
/// <param name="slots">The slot bag, pre-filled at recipe start and on resume.</param>
/// <param name="stepIndex">The step to resume at (0 for a fresh recipe).</param>
/// <param name="needsConfirmation">True when the recipe was matched via the semantic fallback (not an explicit trigger keyword) and must be confirmed by the user before its steps are forced.</param>
/// <param name="goal">The recipe's English goal description, surfaced to the model when phrasing the confirmation question.</param>
/// <param name="alternativeGoal">Goal of a semantically almost-as-close second recipe, surfaced in the confirmation question so the user can pick between the two instead of the engine silently choosing.</param>
/// <param name="captureRewindUsed">True when a prior turn already spent this plan's one-shot ambiguous-capture rewind; restored on resume so the guard is not re-armed each turn.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Extensions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class RecipeExecutionPlan : IRecipeForcingPlan
{
    private const string ResultDataMarker = "Data: ";
    private const string ArrayMarker = "[].";

    private readonly IReadOnlyList<RecipeStep> _steps;
    private readonly Dictionary<string, string> _slots;
    private int _index;
    private bool _deactivated;
    private bool _captureRewindUsed;

    public RecipeExecutionPlan(
        string name,
        IReadOnlyList<RecipeStep> steps,
        Dictionary<string, string>? slots = null,
        int stepIndex = 0,
        bool needsConfirmation = false,
        string? goal = null,
        string? alternativeGoal = null,
        bool captureRewindUsed = false,
        IReadOnlyDictionary<string, string>? goalTranslations = null,
        IReadOnlyDictionary<string, string>? alternativeGoalTranslations = null)
    {
        Name = name;
        Goal = string.IsNullOrWhiteSpace(goal) ? name : goal;
        AlternativeGoal = string.IsNullOrWhiteSpace(alternativeGoal) ? null : alternativeGoal;
        GoalTranslations = goalTranslations;
        AlternativeGoalTranslations = alternativeGoalTranslations;
        _steps = steps;
        _slots = slots ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _index = stepIndex;
        NeedsConfirmation = needsConfirmation;
        _captureRewindUsed = captureRewindUsed;
    }

    public string Name { get; }

    public string Goal { get; }

    public string? AlternativeGoal { get; }

    /// <summary>
    /// Localized goal texts (language code to text) authored in the recipe seed, used by the
    /// deterministic confirmation fallback so the user never sees the raw English goal.
    /// </summary>
    public IReadOnlyDictionary<string, string>? GoalTranslations { get; }

    /// <summary>
    /// Localized goal texts of the almost-as-close second recipe surfaced in the confirmation gate.
    /// </summary>
    public IReadOnlyDictionary<string, string>? AlternativeGoalTranslations { get; }

    /// <summary>
    /// The fully-formatted system instruction for the confirmation turn. When a second recipe matched
    /// almost as closely, it uses the two-choice template so the question offers both interpretations
    /// instead of a plain yes/no — a wrong top pick must not silently railroad the user into the wrong
    /// guided flow. Otherwise it uses the plain yes/no template. Centralized here so both chat paths
    /// stay coherent (no contradictory "yes/no" plus "pick which of the two" in one prompt).
    /// </summary>
    public string ConfirmationInstruction => AlternativeGoal == null
        ? string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            RecipeEngineDefaults.ConfirmationStepInstructionTemplate, Goal)
        : string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            RecipeEngineDefaults.ConfirmationStepWithAlternativeInstructionTemplate, Goal, AlternativeGoal);

    /// <summary>
    /// True while a semantically-matched recipe is still awaiting the user's go-ahead. The chat loop
    /// must ask a confirmation question and pause instead of forcing any step while this is true.
    /// </summary>
    public bool NeedsConfirmation { get; private set; }

    public void ConfirmAndProceed()
    {
        NeedsConfirmation = false;
    }

    public IReadOnlyDictionary<string, string> Slots => _slots;

    public int StepIndex => _index;

    /// <summary>
    /// True once the one-shot ambiguous-capture rewind has been spent. Persisted with the pending
    /// recipe so the guard survives a resume rather than re-arming every turn.
    /// </summary>
    public bool CaptureRewindUsed => _captureRewindUsed;

    public bool IsActive => !_deactivated && _index < _steps.Count;

    public RecipeStep? CurrentStep => IsActive ? _steps[_index] : null;

    public bool CurrentIsAsk => IsActive && string.Equals(_steps[_index].Kind, RecipeStepKinds.Ask, StringComparison.OrdinalIgnoreCase);

    public string? CurrentSkill => IsActive ? _steps[_index].Skill : null;

    public string? CurrentStepNote
    {
        get
        {
            if (!IsActive)
            {
                return null;
            }

            var parts = new[] { _steps[_index].Note, GetKnownValuesNote() }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var combined = string.Join("\n", parts);
            return string.IsNullOrWhiteSpace(combined) ? null : combined;
        }
    }

    public string? CurrentAskPrompt => CurrentIsAsk ? _steps[_index].Prompt : null;

    /// <summary>
    /// Localized user-facing questions (language code to text) authored for the current ask step,
    /// used by the deterministic ask fallback instead of the English meta-prompt.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CurrentAskPromptTranslations =>
        CurrentIsAsk ? _steps[_index].PromptTranslations : null;

    public void PrefillSlots(IReadOnlyDictionary<string, string> extracted)
    {
        foreach (var (key, value) in extracted)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _slots[key] = value;
            }
        }
    }

    public void FillSlot(string slot, string value)
    {
        if (!string.IsNullOrWhiteSpace(slot) && !string.IsNullOrWhiteSpace(value))
        {
            _slots[slot] = value;
        }
    }

    public IReadOnlyDictionary<string, string> AskSlotHints()
    {
        var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in _steps)
        {
            if (string.Equals(step.Kind, RecipeStepKinds.Ask, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(step.Slot)
                && !hints.ContainsKey(step.Slot))
            {
                hints[step.Slot] = step.Description ?? step.Prompt ?? step.Slot;
            }
        }

        return hints;
    }

    /// <summary>
    /// Advance past steps whose work is already done: an ask whose slot is filled, or a search whose
    /// capture target slot is filled. Stops at the first step that still needs the model (an unfilled
    /// ask, or any push step). Returns when no more steps can be auto-satisfied.
    /// </summary>
    public void AdvanceOverSatisfied()
    {
        while (IsActive)
        {
            var step = _steps[_index];

            if (string.Equals(step.Kind, RecipeStepKinds.Ask, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(step.Slot) && _slots.ContainsKey(step.Slot))
                {
                    _index++;
                    continue;
                }
                return;
            }

            if (string.Equals(step.Kind, RecipeStepKinds.Search, StringComparison.OrdinalIgnoreCase))
            {
                var captureSlot = ParseCaptureSlot(step.Capture);
                if (captureSlot != null && _slots.ContainsKey(captureSlot))
                {
                    _index++;
                    continue;
                }
            }

            return;
        }
    }

    public IReadOnlyDictionary<string, object> GetParameterInjections(string skillName)
    {
        if (!IsActive
            || !string.Equals(skillName, _steps[_index].Skill, StringComparison.OrdinalIgnoreCase)
            || _steps[_index].Inject == null)
        {
            return EmptyInjections;
        }

        var result = new Dictionary<string, object>();
        foreach (var (param, reference) in _steps[_index].Inject!)
        {
            var value = ResolveReference(reference);
            if (value != null)
            {
                result[param] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Filled slots that are NOT injected into the current step, surfaced to the model as known values
    /// so it can format free-text inputs (e.g. a date the user gave) into the forced skill's parameters.
    /// </summary>
    public string? GetKnownValuesNote()
    {
        if (!IsActive)
        {
            return null;
        }

        var injectedRefs = _steps[_index].Inject?.Values
            .Select(TryGetSlotName)
            .Where(s => s != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var known = _slots
            .Where(kv => !injectedRefs.Contains(kv.Key))
            .Select(kv => $"{kv.Key} = {kv.Value}")
            .ToList();

        return known.Count == 0 ? null : "Known values from the conversation: " + string.Join("; ", known) + ".";
    }

    public void Observe(IEnumerable<LLMFunctionCall> executedCalls)
    {
        if (!IsActive)
        {
            return;
        }

        var step = _steps[_index];
        var call = executedCalls.FirstOrDefault(c =>
            string.Equals(c.FunctionName, step.Skill, StringComparison.OrdinalIgnoreCase) && c.Success);
        if (call == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(step.Capture))
        {
            var (slot, value) = ExtractCapture(step.Capture, call.Result);
            if (slot == null || value == null)
            {
                if (!TryRewindToDisambiguate(step))
                {
                    _deactivated = true;
                }

                return;
            }

            _slots[slot] = value;
        }

        _index++;
    }

    /// <summary>
    /// Recovery path for an ambiguous capture (zero or many result rows): instead of silently
    /// deactivating the whole recipe, clear the input slot that fed the search and rewind to its ask
    /// step, so the user is asked again and can disambiguate (the candidate list from the search result
    /// is already in the conversation history). One rewind per recipe LIFETIME, not per turn: the spent
    /// flag is persisted with the pending recipe (see <see cref="CaptureRewindUsed"/>) and restored on
    /// resume, so a repeatedly ambiguous answer deactivates on the second attempt instead of re-asking
    /// forever. The budget is plan-global and shared across all search steps — if an early search spends
    /// it, a later ambiguous search in the same recipe deactivates with no recovery. That is strictly
    /// better than the previous behavior (zero rewinds); a per-search-step budget would need a per-slot
    /// marker instead of this single bool.
    /// </summary>
    private bool TryRewindToDisambiguate(RecipeStep searchStep)
    {
        if (_captureRewindUsed || searchStep.Inject == null)
        {
            return false;
        }

        foreach (var reference in searchStep.Inject.Values)
        {
            var slot = TryGetSlotName(reference);
            if (slot == null)
            {
                continue;
            }

            var askIndex = FindAskStepIndex(slot);
            if (askIndex < 0)
            {
                continue;
            }

            _captureRewindUsed = true;
            _slots.Remove(slot);
            _index = askIndex;
            return true;
        }

        return false;
    }

    private int FindAskStepIndex(string slot)
    {
        for (var i = 0; i < _steps.Count; i++)
        {
            if (string.Equals(_steps[i].Kind, RecipeStepKinds.Ask, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_steps[i].Slot, slot, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Resolves an inject value: a $-prefixed reference is looked up in the slot bag (null when the
    /// slot is unfilled); any other non-empty value is passed through verbatim as a literal constant,
    /// so recipes can pin skill parameters (e.g. entityType) deterministically instead of relying on
    /// the model to honor a note.
    /// </summary>
    private object? ResolveReference(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        var slot = TryGetSlotName(reference);
        if (slot == null)
        {
            return reference;
        }

        return _slots.TryGetValue(slot, out var value) ? value : null;
    }

    private static string? TryGetSlotName(string reference)
    {
        if (string.IsNullOrEmpty(reference)
            || !reference.StartsWith(RecipeEngineDefaults.SlotReferencePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return reference[RecipeEngineDefaults.SlotReferencePrefix.Length..];
    }

    private static string? ParseCaptureSlot(string? capture)
    {
        if (string.IsNullOrWhiteSpace(capture))
        {
            return null;
        }

        var separatorIndex = capture.IndexOf(RecipeEngineDefaults.CaptureSeparator, StringComparison.OrdinalIgnoreCase);
        return separatorIndex < 0 ? null : capture[(separatorIndex + RecipeEngineDefaults.CaptureSeparator.Length)..].Trim();
    }

    /// <summary>
    /// Parses a capture spec "ArrayProp[].IdProp as slot" and pulls the lone element's id from the
    /// skill result. Returns (slot, value) only when the array holds exactly one element, mirroring the
    /// proven cut-recipe rule: an ambiguous result (zero or many) yields no capture and deactivates.
    /// </summary>
    private static (string? slot, string? value) ExtractCapture(string captureSpec, string? result)
    {
        var separatorIndex = captureSpec.IndexOf(RecipeEngineDefaults.CaptureSeparator, StringComparison.OrdinalIgnoreCase);
        if (separatorIndex < 0)
        {
            return (null, null);
        }

        var path = captureSpec[..separatorIndex].Trim();
        var slot = captureSpec[(separatorIndex + RecipeEngineDefaults.CaptureSeparator.Length)..].Trim();

        var arrayMarkerIndex = path.IndexOf(ArrayMarker, StringComparison.Ordinal);
        if (arrayMarkerIndex < 0)
        {
            return (null, null);
        }

        var arrayProp = path[..arrayMarkerIndex];
        var idProp = path[(arrayMarkerIndex + ArrayMarker.Length)..];

        if (string.IsNullOrEmpty(result))
        {
            return (null, null);
        }

        var markerIndex = result.IndexOf(ResultDataMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return (null, null);
        }

        var json = result[(markerIndex + ResultDataMarker.Length)..];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetPropertyCaseInsensitive(arrayProp, out var array)
                || array.ValueKind != JsonValueKind.Array
                || array.GetArrayLength() != 1)
            {
                return (null, null);
            }

            if (array[0].TryGetPropertyCaseInsensitive(idProp, out var id))
            {
                var value = id.ValueKind == JsonValueKind.String ? id.GetString() : id.GetRawText();
                return string.IsNullOrWhiteSpace(value) ? (null, null) : (slot, value);
            }
        }
        catch (JsonException)
        {
            return (null, null);
        }

        return (null, null);
    }

    private static readonly IReadOnlyDictionary<string, object> EmptyInjections =
        new Dictionary<string, object>();
}
