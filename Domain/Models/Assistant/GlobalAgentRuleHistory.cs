// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class GlobalAgentRuleHistory : BaseEntity
{
    public Guid GlobalAgentRuleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContentBefore { get; set; }

    public string ContentAfter { get; set; } = string.Empty;

    public int Version { get; set; }

    public string ChangeType { get; set; } = "update";

    public string? ChangedBy { get; set; }

    public string? ChangeReason { get; set; }

    public virtual GlobalAgentRule GlobalAgentRule { get; set; } = null!;
}
