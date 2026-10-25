// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A deterministic, in-flight recipe: an ordered list of step skills the chat loop forces one at a time
/// (the "B backbone"). The plan advances only when the current step's skill actually executed AND
/// succeeded. Values flow between forced steps in code, not through the model (the chat-loop equivalent of the plan
/// engine's $prev, which is fragile across small models): a customer-capturing step (find_customer_candidates) captures
/// the lone matching clientId and a customer-needing step (create_shift) gets it injected; an order-capturing step
/// (create_shift) captures the created order id and the seal step (seal_shift) gets it injected as shiftId; the seal
/// yields the plannable shift id and the cut step (cut_shift) gets that one injected. If the customer is ambiguous (not
/// exactly one match) the plan deactivates and the normal conversational path takes over.
/// A plan object lives for one chat turn. The seal is held by the autonomy gate in a chat turn, so the plan stops there
/// (AwaitsConfirmation) and the turn that confirms the seal builds a new plan on the seal step that waits for the
/// confirmed replay (confirm_pending_action replaying seal_shift) instead of forcing the seal again
/// (CutRecipeContinuationResolver). A refused seal, a failed replay or a replay of another order ends the plan
/// (IsStoppedAtSeal).
/// </summary>
/// <param name="name">The recipe identifier (e.g. "dienst-aus-bestellung-schneiden").</param>
/// <param name="steps">The ordered steps.</param>
/// <param name="startIndex">The first step to force (allows skipping resolved steps, e.g. when the clientId is already known).</param>
/// <param name="initialClientId">A pre-known clientId (e.g. supplied as a GUID in the request), injected into the customer-needing step.</param>
/// <param name="initialOrderId">A pre-known order id (the order the previous turn created), injected into the seal step and compared with the order a confirmed seal replay sealed.</param>
/// <param name="awaitsConfirmedReplay">True when the plan starts on the seal step of a turn that redeems the held seal: the step then waits for the confirmed replay instead of forcing seal_shift.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Extensions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class RecipeForcingPlan : IRecipeForcingPlan
{
    private const string ResultDataMarker = "Data: ";
    private const string CustomersProperty = "Customers";
    private const string CustomerIdProperty = "CustomerId";

    private readonly IReadOnlyList<RecipeForcingStep> _steps;
    private int _index;
    private bool _deactivated;
    private bool _stoppedAtSeal;
    private bool _awaitsConfirmedReplay;
    private string? _capturedClientId;
    private string? _capturedOrderId;
    private string? _capturedPlannableShiftId;

    public RecipeForcingPlan(
        string name,
        IReadOnlyList<RecipeForcingStep> steps,
        int startIndex = 0,
        string? initialClientId = null,
        string? initialOrderId = null,
        bool awaitsConfirmedReplay = false)
    {
        Name = name;
        _steps = steps;
        _index = startIndex;
        _capturedClientId = initialClientId;
        _capturedOrderId = initialOrderId;
        _awaitsConfirmedReplay = awaitsConfirmedReplay;
    }

    public string Name { get; }

    public bool IsActive => !_deactivated && !_stoppedAtSeal && _index < _steps.Count;

    /// <summary>True when the plan stopped because the customer match was ambiguous, not because all steps ran.</summary>
    public bool IsDeactivated => _deactivated;

    /// <summary>
    /// True when the plan stopped on its seal step: the seal was refused (for example an order without a group), the
    /// confirmed replay failed, or the replay sealed another order than the one this recipe created.
    /// </summary>
    public bool IsStoppedAtSeal => _stoppedAtSeal;

    /// <summary>
    /// True once the autonomy gate held the seal step of this turn: the plan waits for the user's confirmation, which a
    /// later turn redeems and continues from (CutRecipeContinuationResolver).
    /// </summary>
    public bool AwaitsConfirmation { get; private set; }

    public int StepIndex => _index;

    public string? CurrentSkill => !IsActive
        ? null
        : _awaitsConfirmedReplay && _steps[_index].SealsOrder
            ? AutonomyDefaults.ConfirmPendingActionSkillName
            : _steps[_index].Skill;

    public string? CurrentStepNote => IsActive ? _steps[_index].StepNote : null;

    public IReadOnlyDictionary<string, object> GetParameterInjections(string skillName)
    {
        if (!IsActive || !string.Equals(skillName, CurrentSkill, StringComparison.OrdinalIgnoreCase))
        {
            return EmptyInjections;
        }

        var step = _steps[_index];
        var injected = step switch
        {
            { NeedsCustomerId: true } => (Key: RecipeConstants.ClientIdParam, Value: _capturedClientId),
            { NeedsOrderId: true } when !_awaitsConfirmedReplay => (Key: RecipeConstants.ShiftIdParam, Value: _capturedOrderId),
            { NeedsPlannableShiftId: true } => (Key: RecipeConstants.ShiftIdParam, Value: _capturedPlannableShiftId),
            _ => (Key: string.Empty, Value: (string?)null)
        };

        return injected.Value == null
            ? EmptyInjections
            : new Dictionary<string, object> { [injected.Key] = injected.Value };
    }

    public void Observe(IEnumerable<LLMFunctionCall> executedCalls)
    {
        if (!IsActive)
        {
            return;
        }

        var step = _steps[_index];
        if (step.SealsOrder)
        {
            ObserveSeal(step, executedCalls);
            return;
        }

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

        if (step.CapturesOrder)
        {
            _capturedOrderId = ExtractDataString(call.Result, RecipeConstants.CreatedOrderIdDataProperty);
        }

        _index++;
    }

    private void ObserveSeal(RecipeForcingStep step, IEnumerable<LLMFunctionCall> executedCalls)
    {
        var expectedSkill = _awaitsConfirmedReplay ? AutonomyDefaults.ConfirmPendingActionSkillName : step.Skill;
        var call = executedCalls.FirstOrDefault(c =>
            string.Equals(c.FunctionName, expectedSkill, StringComparison.OrdinalIgnoreCase));
        if (call == null)
        {
            return;
        }

        if (!_awaitsConfirmedReplay && call.RequiresConfirmation)
        {
            AwaitsConfirmation = true;
            return;
        }

        var ranTheSeal = call.Success
            && (!_awaitsConfirmedReplay
                || string.Equals(call.ReplayedSkillName, step.Skill, StringComparison.OrdinalIgnoreCase));
        var sealedOrderId = ranTheSeal ? ExtractDataString(call.Result, RecipeConstants.SealedOrderIdDataProperty) : null;
        var plannableShiftId = ranTheSeal ? ExtractDataString(call.Result, RecipeConstants.PlannableShiftIdDataProperty) : null;
        var isThisRecipesOrder = _capturedOrderId == null
            || string.Equals(sealedOrderId, _capturedOrderId, StringComparison.OrdinalIgnoreCase);
        if (!ranTheSeal || plannableShiftId == null || !isThisRecipesOrder)
        {
            _stoppedAtSeal = true;
            return;
        }

        _capturedPlannableShiftId = plannableShiftId;
        _awaitsConfirmedReplay = false;
        AwaitsConfirmation = false;
        _index++;
    }

    private static string? ExtractDataString(string? result, string property)
    {
        using var doc = ParseResultData(result);
        if (doc == null || !doc.RootElement.TryGetPropertyCaseInsensitive(property, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? ExtractLoneCustomerId(string? result)
    {
        using var doc = ParseResultData(result);
        if (doc == null
            || !doc.RootElement.TryGetPropertyCaseInsensitive(CustomersProperty, out var customers)
            || customers.ValueKind != JsonValueKind.Array
            || customers.GetArrayLength() != 1)
        {
            return null;
        }

        var only = customers[0];
        if (only.TryGetPropertyCaseInsensitive(CustomerIdProperty, out var id))
        {
            var value = id.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static JsonDocument? ParseResultData(string? result)
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

        try
        {
            var document = JsonDocument.Parse(result[(markerIndex + ResultDataMarker.Length)..]);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return document;
            }

            document.Dispose();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly IReadOnlyDictionary<string, object> EmptyInjections =
        new Dictionary<string, object>();
}
