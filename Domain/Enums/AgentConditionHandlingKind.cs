// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How a condition-ledger row was ultimately handled once it moved past Detected. None is the
/// fail-closed default for a freshly detected row; it only advances once the pipeline actually acts.
/// </summary>
public enum AgentConditionHandlingKind
{
    None = 0,
    Hint = 1,
    ScenarioPrepared = 2,
    Executed = 3
}
