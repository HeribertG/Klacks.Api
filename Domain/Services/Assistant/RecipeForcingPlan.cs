// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A deterministic, in-flight recipe: an ordered list of step skills the chat loop forces one at a time
/// (the "B backbone"). The plan advances only when the current step's skill actually executed AND
/// succeeded. A customer-capturing step (find_customer_candidates) deterministically captures the lone
/// matching clientId from its result; a customer-needing step (create_shift) gets that id injected into
/// its parameters before execution — this is the reliable, in-code data flow between forced steps (the
/// chat-loop equivalent of the plan engine's $prev, which is fragile across small models). If the
/// customer is ambiguous (not exactly one match) the plan deactivates and the normal conversational path
/// takes over. Lives for a single chat turn.
/// </summary>
/// <param name="name">The recipe identifier (e.g. "dienst-aus-bestellung-schneiden").</param>
/// <param name="steps">The ordered steps.</param>
/// <param name="startIndex">The first step to force (allows skipping resolved steps, e.g. when the clientId is already known).</param>
/// <param name="initialClientId">A pre-known clientId (e.g. supplied as a GUID in the request), injected into the customer-needing step.</param>

using System.Text.Json;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class RecipeForcingPlan
{
    private const string ResultDataMarker = "Data: ";
    private const string CustomersProperty = "Customers";
    private const string CustomerIdProperty = "CustomerId";
    private const string ClientIdInjectionKey = "clientId";

    private readonly IReadOnlyList<RecipeForcingStep> _steps;
    private int _index;
    private bool _deactivated;
    private string? _capturedClientId;

    public RecipeForcingPlan(
        string name,
        IReadOnlyList<RecipeForcingStep> steps,
        int startIndex = 0,
        string? initialClientId = null)
    {
        Name = name;
        _steps = steps;
        _index = startIndex;
        _capturedClientId = initialClientId;
    }

    public string Name { get; }

    public bool IsActive => !_deactivated && _index < _steps.Count;

    public string? CurrentSkill => IsActive ? _steps[_index].Skill : null;

    public string? CurrentStepNote => IsActive ? _steps[_index].StepNote : null;

    public IReadOnlyDictionary<string, object> GetParameterInjections(string skillName)
    {
        if (IsActive
            && _steps[_index].NeedsCustomerId
            && _capturedClientId != null
            && string.Equals(skillName, _steps[_index].Skill, StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, object> { [ClientIdInjectionKey] = _capturedClientId };
        }

        return EmptyInjections;
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

        if (step.CapturesCustomer)
        {
            _capturedClientId = ExtractLoneCustomerId(call.Result);
            if (_capturedClientId == null)
            {
                _deactivated = true;
                return;
            }
        }

        _index++;
    }

    private static string? ExtractLoneCustomerId(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        var markerIndex = result.IndexOf(ResultDataMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var json = result[(markerIndex + ResultDataMarker.Length)..];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!TryGetPropertyCaseInsensitive(doc.RootElement, CustomersProperty, out var customers)
                || customers.ValueKind != JsonValueKind.Array
                || customers.GetArrayLength() != 1)
            {
                return null;
            }

            var only = customers[0];
            if (TryGetPropertyCaseInsensitive(only, CustomerIdProperty, out var id))
            {
                var value = id.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
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
