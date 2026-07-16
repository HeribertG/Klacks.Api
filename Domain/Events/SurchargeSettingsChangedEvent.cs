// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

/// <summary>
/// Raised after a committed change to one or more global settings whose values feed the persisted
/// surcharge or overtime calculation (see SurchargeRelevantSettingKeys). Global settings carry no
/// validity date, so the affected window is the full unlocked real-mode work range.
/// </summary>
/// <param name="ChangedKeys">The surcharge-relevant setting keys whose stored value actually changed</param>
public sealed record SurchargeSettingsChangedEvent(IReadOnlyCollection<string> ChangedKeys) : DomainEvent;
