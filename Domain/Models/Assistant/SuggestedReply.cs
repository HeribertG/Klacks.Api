// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public class SuggestedReply
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
