// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class LlmRepliesFormat
{
    public const string BlockPrefix = "[REPLIES:";
    public const string ModeSingle = "single";
    public const string ModeMulti = "multi";
    public const string ModeDate = "date";
    public const string ModeNumber = "number";
    public const int MaxOptions = 10;
    public const string ConfirmYesValue = "yes";
    public const string ConfirmNoValue = "no";
}
