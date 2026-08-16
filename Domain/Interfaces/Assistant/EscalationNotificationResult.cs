// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="Outcome">The messenger attempt's verdict; the loud A1 channel decides delivery success.</param>
/// <param name="DispatchRowId">The inbox row this attempt produced, for EscalationStage.DispatchRowId.</param>
/// <param name="Channel">Messenger channel that was addressed, if any.</param>
public readonly record struct EscalationNotificationResult(
    OfflineMessengerDeliveryOutcome Outcome, Guid DispatchRowId, string? Channel);
