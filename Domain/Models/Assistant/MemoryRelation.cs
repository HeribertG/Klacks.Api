// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class MemoryRelation : BaseEntity
{
    public Guid AgentId { get; set; }

    public Guid MemoryAId { get; set; }

    public Guid MemoryBId { get; set; }

    public MemoryRelationType Type { get; set; }

    public double Confidence { get; set; }

    public string Provenance { get; set; } = string.Empty;

    public MemoryRelationStatus Status { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual AgentMemory MemoryA { get; set; } = null!;

    public virtual AgentMemory MemoryB { get; set; } = null!;
}
