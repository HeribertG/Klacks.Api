// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.ValueObjects;

/// <summary>
/// Canonical, order-independent representation of an undirected memory-to-memory edge: the smaller
/// id is always MemoryAId, so (A,B) and (B,A) resolve to the same pair and can never be stored twice.
/// </summary>
public readonly record struct MemoryRelationPair(Guid MemoryAId, Guid MemoryBId)
{
    public static MemoryRelationPair Canonical(Guid memoryId1, Guid memoryId2)
    {
        return memoryId1.CompareTo(memoryId2) <= 0
            ? new MemoryRelationPair(memoryId1, memoryId2)
            : new MemoryRelationPair(memoryId2, memoryId1);
    }
}
