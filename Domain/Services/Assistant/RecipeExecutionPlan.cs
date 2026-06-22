// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A data-driven recipe in flight: an ordered list of steps with a slot bag carried between them.
/// Two step directions: "pull" (ask) emits a question and stops the turn so the slot bag persists;
/// "push" (search/mutate) forces a skill, injects slot values into its parameters, captures a value
/// from its result into a slot, and advances within the same turn. A step is skipped when its slot
/// is already filled — ask whose slot is set, or search whose capture target is set — which is how
/// "don't re-ask what was already said" and the GUID fast-path fall out of one rule.
/// </summary>
/// <param name="name">The recipe name.</param>
/// <param name="steps">The ordered steps (deserialized from the recipe definition).</param>
/// <param name="slots">The slot bag, pre-filled at recipe start and on resume.</param>
/// <param name="stepIndex">The step to resume at (0 for a fresh recipe).</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
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

    public RecipeExecutionPlan(
        string name,
        IReadOnlyList<RecipeStep> steps,
        Dictionary<string, string>? slots = null,
        int stepIndex = 0)
    {
        Name = name;
        _steps = steps;
        _slots = slots ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _index = stepIndex;
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> Slots => _slots;

    public int StepIndex => _index;

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
            .Select(StripPrefix)
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
                _deactivated = true;
                return;
            }

            _slots[slot] = value;
        }

        _index++;
    }

    private object? ResolveReference(string reference)
    {
        var slot = StripPrefix(reference);
        if (slot != null && _slots.TryGetValue(slot, out var value))
        {
            return value;
        }

        return null;
    }

    private static string? StripPrefix(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return reference.StartsWith(RecipeEngineDefaults.SlotReferencePrefix, StringComparison.Ordinal)
            ? reference[RecipeEngineDefaults.SlotReferencePrefix.Length..]
            : reference;
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
            if (!TryGetPropertyCaseInsensitive(doc.RootElement, arrayProp, out var array)
                || array.ValueKind != JsonValueKind.Array
                || array.GetArrayLength() != 1)
            {
                return (null, null);
            }

            if (TryGetPropertyCaseInsensitive(array[0], idProp, out var id))
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

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static readonly IReadOnlyDictionary<string, object> EmptyInjections =
        new Dictionary<string, object>();
}
