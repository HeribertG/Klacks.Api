// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mutes or unmutes one trigger kind for one user. Muting is an explicit acknowledgement (F1 of the
/// proactive completion plan): the user has decided about this kind, so every one of their still open
/// dispatch rows of that kind is acknowledged as well and drops out of the reminder loop for good. The
/// preference gate alone would only defer those rows, so after an unmute the loop would resume and nag
/// about findings the user already settled.
///
/// Two deliberate boundaries:
/// (1) Only muting acknowledges. Unmuting is the same command with Muted = false and must never touch
///     the dispatch rows, or re-enabling a kind would wipe the user's open messages.
/// (2) Snoozing does NOT acknowledge and therefore does not run through this command. A snooze means
///     "not now", the reminder is supposed to come back, and IsAllowedAsync gates it independently of
///     Muted.
///
/// The acknowledgement is part of the mute, not a best-effort side effect: a failing acknowledgement
/// fails the whole command rather than leaving a muted kind whose rows keep their reminder schedule.
/// The preference is written first because both steps are idempotent and the client retries, so a
/// failure after the mute self-heals, while the reverse order could strip reminders without ever
/// delivering the mute. Returns how many rows the mute acknowledged (0 when unmuting).
/// </summary>
/// <param name="preferenceService">Per-user mute state the trigger and reminder gates read.</param>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class MuteTriggerKindCommandHandler : IRequestHandler<MuteTriggerKindCommand, int>
{
    private readonly IAgentTriggerPreferenceService _preferenceService;
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public MuteTriggerKindCommandHandler(
        IAgentTriggerPreferenceService preferenceService,
        IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _preferenceService = preferenceService;
        _dispatchRepository = dispatchRepository;
    }

    public async Task<int> Handle(MuteTriggerKindCommand request, CancellationToken cancellationToken)
    {
        await _preferenceService.MuteAsync(request.UserId, request.TriggerKind, request.Muted);

        if (!request.Muted)
        {
            return 0;
        }

        return await _dispatchRepository.AcknowledgeAllForKindAsync(
            request.UserId, request.TriggerKind, cancellationToken);
    }
}
