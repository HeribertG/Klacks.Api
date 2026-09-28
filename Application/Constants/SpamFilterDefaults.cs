// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

public static class SpamFilterDefaults
{
    public const float SpamThreshold = 0.7f;
    public const float UncertainThreshold = 0.4f;
    public const int MaxBodyLengthForLlm = 500;
}
