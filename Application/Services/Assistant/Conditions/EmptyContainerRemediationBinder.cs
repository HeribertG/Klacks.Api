// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns an empty_container condition's payload into create_container_template's arguments. Pure, as
/// IConditionRemediationParameterBinder requires: everything it needs was captured into the payload by
/// EmptyContainerDetector when the ledger row was opened, so it never reads a Shift.
///
/// The weekday is the LOWEST ISO weekday the container runs on. create_container_template writes one
/// weekday per call while a container commonly runs on several, so a single automated remediation can
/// only ever configure the first of them - which is enough to clear THIS finding, because
/// EmptyContainerDetector fires on "no template at all", not on "a template per weekday". The remaining
/// weekdays stay unconfigured and are not detected by anything today; that is a gap in the detector's
/// resolution, not something this binder can paper over.
///
/// Unbindable rather than wrong, in three cases, each of which returns an INCOMPLETE dictionary that the
/// dispatcher's required-argument pre-flight rejects before it claims the row - so an unbindable
/// condition costs neither an attempt nor a slot of the daily action budget:
/// (a) a payload written before Etappe 5b, which carries no schedule at all. This case is now TRANSIENT:
///     since the payload refresh of 2026-08-26 an open row picks up the current payload shape on the next
///     tick that still reports it, so such a row recovers by itself instead of staying unbindable for the
///     rest of its life;
/// (b) a container with no weekday flag set, where there is no weekday to write;
/// (c) a container whose EndShift is not after its StartShift (a night container crossing midnight),
///     which create_container_template refuses by its own validation. Emitting the pair anyway would
///     buy three guaranteed skill failures and an escalation instead of one quiet skip.
/// </summary>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class EmptyContainerRemediationBinder : IConditionRemediationParameterBinder
{
    private const int IsoWeekdayMinimum = 1;
    private const int IsoWeekdayMaximum = 7;

    public IReadOnlyDictionary<string, object?> Bind(IReadOnlyDictionary<string, object?> conditionPayload)
    {
        ArgumentNullException.ThrowIfNull(conditionPayload);

        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (TryReadGuid(conditionPayload, EmptyContainerPayloadKeys.ShiftId, out var containerId))
        {
            arguments[CreateContainerTemplateParameters.ContainerId] = containerId.ToString();
        }

        if (TryReadLowestIsoWeekday(conditionPayload, out var isoWeekday))
        {
            arguments[CreateContainerTemplateParameters.Weekday] = isoWeekday;
        }

        if (TryReadTime(conditionPayload, EmptyContainerPayloadKeys.StartShift, out var fromTime)
            && TryReadTime(conditionPayload, EmptyContainerPayloadKeys.EndShift, out var untilTime)
            && untilTime > fromTime)
        {
            arguments[CreateContainerTemplateParameters.FromTime] = Format(fromTime);
            arguments[CreateContainerTemplateParameters.UntilTime] = Format(untilTime);
        }

        arguments[CreateContainerTemplateParameters.IsHoliday] =
            ReadBoolean(conditionPayload, EmptyContainerPayloadKeys.IsHoliday);
        arguments[CreateContainerTemplateParameters.IsWeekdayAndHoliday] =
            ReadBoolean(conditionPayload, EmptyContainerPayloadKeys.IsWeekdayAndHoliday);

        return arguments;
    }

    private static string Format(TimeOnly time) =>
        time.ToString(CreateContainerTemplateParameters.TimeFormat, CultureInfo.InvariantCulture);

    private static bool TryReadGuid(IReadOnlyDictionary<string, object?> payload, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = ReadScalar(payload, key);

        return raw switch
        {
            Guid guidValue => Assign(guidValue, out value),
            string text => Guid.TryParse(text, out value),
            _ => false
        };
    }

    private static bool TryReadTime(IReadOnlyDictionary<string, object?> payload, string key, out TimeOnly value)
    {
        value = default;
        var raw = ReadScalar(payload, key);

        return raw switch
        {
            TimeOnly timeValue => Assign(timeValue, out value),
            string text => TimeOnly.TryParse(text, CultureInfo.InvariantCulture, out value),
            _ => false
        };
    }

    private static bool ReadBoolean(IReadOnlyDictionary<string, object?> payload, string key) =>
        ReadScalar(payload, key) is bool flag && flag;

    /// <summary>
    /// The lowest ISO weekday the container runs on. Accepts both shapes the payload can arrive in: a
    /// JsonElement array when it came back out of PayloadJson, and a plain integer sequence when a
    /// caller handed the dictionary over directly.
    /// </summary>
    private static bool TryReadLowestIsoWeekday(IReadOnlyDictionary<string, object?> payload, out int isoWeekday)
    {
        isoWeekday = 0;
        if (!payload.TryGetValue(EmptyContainerPayloadKeys.IsoWeekdays, out var raw) || raw is null)
        {
            return false;
        }

        var weekdays = raw switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.Array =>
                element.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.Number)
                    .Select(item => item.GetInt32()),
            IEnumerable<int> typed => typed,
            _ => null
        };

        if (weekdays is null)
        {
            return false;
        }

        var candidates = weekdays.Where(IsIsoWeekday).ToList();
        if (candidates.Count == 0)
        {
            return false;
        }

        isoWeekday = candidates.Min();
        return true;
    }

    private static bool IsIsoWeekday(int value) => value >= IsoWeekdayMinimum && value <= IsoWeekdayMaximum;

    private static object? ReadScalar(IReadOnlyDictionary<string, object?> payload, string key) =>
        payload.TryGetValue(key, out var raw)
            ? SkillParameterValueUnwrapper.Unwrap(raw)
            : null;

    private static bool Assign<T>(T source, out T target)
    {
        target = source;
        return true;
    }
}
