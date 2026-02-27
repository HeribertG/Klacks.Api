// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public static class SpamFilterDefaults
{
    public const float SpamThreshold = 0.7f;
    public const float UncertainThreshold = 0.4f;
    public const int MaxBodyLengthForLlm = 500;
}
