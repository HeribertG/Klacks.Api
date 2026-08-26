// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Limits for the condition-attribution lookup the service grid uses, shared between the REST endpoint
/// that rejects an oversized request and the tests that pin the boundary. Sized for a grid page rather
/// than for an inbox page, which is why it does not reuse ProactiveInboxDefaults: a container view sends
/// one id per visible cell, so a month across several groups is an ordinary request here, not an abuse.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ConditionAttributionDefaults
{
    public const int MaxEntityIdsPerRequest = 2000;
}
