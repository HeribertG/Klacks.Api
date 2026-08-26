// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an active, non-scenario container shift (ShiftType.IsContainer,
/// ShiftStatus.OriginalShift) has zero ContainerTemplate rows -- a slot-definition gap the
/// planner has not configured at all. Distinct from unstaffed_shift, which flags missing
/// employees on slots that already exist; EmptyContainerDetector emits one event per container.
/// GroupIds carries every group the container belongs to, which is what narrows the audience to the
/// planners who may see it; a container with no group membership at all reaches Admins only
/// (RequiresGroupScope).
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record EmptyContainerTriggerEvent(
    Guid ShiftId,
    string ContainerName,
    DateOnly FromDate,
    DateOnly? UntilDate,
    IReadOnlyCollection<Guid> GroupIds,
    ContainerScheduleSnapshot Schedule) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.EmptyContainer;

    public string Severity => IsPeriodActive(FromDate, UntilDate)
        ? AgentTriggerSeverity.High
        : AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public bool RequiresGroupScope => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.EmptyContainer;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["name"] = ContainerName,
        ["date"] = FromDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(ShiftId);

    public Guid? EntityId => ShiftId;

    /// <summary>
    /// The DedupKey spelling as a function of its key field, so EmptyContainerDetector's uncapped
    /// fingerprint scan can build the identical key from a key-only projection instead of restating
    /// the format.
    /// </summary>
    public static string DedupKeyFor(Guid shiftId) => shiftId.ToString();

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Date] = FromDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Also the remediation input, not only a description of the finding: the Etappe 5b binder turns
    /// Schedule into create_container_template's arguments, and it may not read the Shift itself.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        [EmptyContainerPayloadKeys.ShiftId] = ShiftId,
        [EmptyContainerPayloadKeys.ContainerName] = ContainerName,
        [EmptyContainerPayloadKeys.FromDate] = FromDate,
        [EmptyContainerPayloadKeys.UntilDate] = UntilDate,
        [EmptyContainerPayloadKeys.GroupIds] = GroupIds,
        [EmptyContainerPayloadKeys.StartShift] = Schedule.StartShift,
        [EmptyContainerPayloadKeys.EndShift] = Schedule.EndShift,
        [EmptyContainerPayloadKeys.IsoWeekdays] = Schedule.IsoWeekdays,
        [EmptyContainerPayloadKeys.IsHoliday] = Schedule.IsHoliday,
        [EmptyContainerPayloadKeys.IsWeekdayAndHoliday] = Schedule.IsWeekdayAndHoliday
    };

    private static bool IsPeriodActive(DateOnly fromDate, DateOnly? untilDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return fromDate <= today && (!untilDate.HasValue || today <= untilDate.Value);
    }
}
