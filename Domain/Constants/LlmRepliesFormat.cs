// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class LlmRepliesFormat
{
    public const string BlockPrefix = "[REPLIES:";
    public const string ModeSingle = "single";
    public const string ModeMulti = "multi";
    public const int MaxOptions = 10;
}
