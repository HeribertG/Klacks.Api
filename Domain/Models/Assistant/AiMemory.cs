// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AiMemory : BaseEntity
{
    public string Category { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Importance { get; set; } = 5;

    public string? Source { get; set; }
}
