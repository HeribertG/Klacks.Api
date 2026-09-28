// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class LlmSuggestionFormat
{
    public const string BlockPrefix = "[SUGGESTIONS:";
    public const string BlockSuffix = "]";
    public const string Separator = "|";
    public const int MaxSuggestions = 4;
    public const int ExpectedSuggestionCount = 3;
}
