// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Size limits for persisted proactive trigger dispatch rows, shared between the EF column
/// configuration and the trigger dispatch service so serialized content params can never
/// overflow the database column.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ProactiveTriggerDispatchLimits
{
    public const int ContentParamsJsonMaxLength = 4000;

    public const int ContentParamValueMaxLength = 1000;

    public const string TruncationSuffix = "…";
}
