// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Body of the "mach du" delegation request on a proactive message.
/// </summary>
/// <param name="MaxAction">
/// The ProactiveMaxAction ordinal (1 Prepare, 2 Execute) to grant for this one condition; 0 Hint is
/// rejected as nothing to delegate, and anything outside the enum is rejected as unknown.
/// </param>

namespace Klacks.Api.Application.DTOs.Assistant;

public class DelegateConditionRequest
{
    public int MaxAction { get; set; }
}
