// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMUsageInfo
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens => InputTokens + OutputTokens;

    public decimal Cost { get; set; }
}
