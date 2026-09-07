// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an installation has never been planned: no work assignment exists anywhere. The stage
/// says how far the order -> shift -> assignment chain got, and each stage renders its own sentence,
/// because a single wording would be factually wrong in at least one of them — telling a planner
/// "no orders have been created" in an installation holding 623 sealed orders is worse than the
/// useless reminder this trigger replaces.
///
/// The DedupKey is the stage alone, and dispatch dedup has no time window
/// (ProactiveTriggerDispatchRepository.WasDispatchedAsync), so a planner sees each stage exactly
/// once — ever, not once per day. That is deliberate: the condition stays true on every tick until
/// somebody plans something, and a daily repetition of "nothing is planned yet" is precisely the
/// kind of noise this whole change removes. Moving from one stage to the next is a new fact and
/// therefore a new message.
/// </summary>
/// <param name="Stage">How far the installation got along the order -> shift -> assignment chain.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record NoScheduleYetTriggerEvent(ScheduleSetupStage Stage) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.NoScheduleYet;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + Stage switch
    {
        ScheduleSetupStage.NothingYet => ProactiveMessageI18nKeys.SetupNothingYet,
        ScheduleSetupStage.OrdersButNoShifts => ProactiveMessageI18nKeys.SetupOrdersButNoShifts,
        ScheduleSetupStage.ShiftsButNoWork => ProactiveMessageI18nKeys.SetupShiftsButNoWork,
        _ => throw new ArgumentOutOfRangeException(nameof(Stage), $"Unsupported setup stage '{Stage}'.")
    };

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>();

    public string DedupKey => Stage.ToString();

    public Guid? GroupId => null;

    /// <summary>
    /// Constant per kind, never derived from Stage. AgentConditionActionRoutes keeps a SEPARATE copy of
    /// this route for ledger rows (AgentCondition does not persist ActionRoute), and its own summary
    /// spells out that an event whose route depends on instance data makes that copy drift silently.
    /// The schedule is the right single answer for all three stages: it is where orders, shifts and
    /// assignments are created, and the stage-specific guidance belongs in the conversation the message
    /// opens, not in the route.
    /// </summary>
    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => null;

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["stage"] = Stage.ToString()
    };
}
