// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The single place that decides who may see and change an agent memory. A memory with
/// <see cref="AgentMemory.UserId"/> null is shared company-wide knowledge; a memory with a value
/// belongs to that one user. Everybody may read shared memories plus their own, nobody reads a
/// foreign personal memory, and only an administrator may change shared memories — the same split
/// the skill layer already enforces (add_personal_memory is open, add/update/delete_ai_memory are
/// administrator-only). A denied access is reported as "not found" by the callers so that the API
/// never confirms the existence of a foreign memory.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AgentMemoryAccessPolicy
{
    /// <summary>
    /// True when the memory is shared company-wide knowledge rather than one user's personal memory.
    /// </summary>
    /// <param name="memory">The memory to classify</param>
    public static bool IsShared(AgentMemory memory) => memory.UserId == null;

    /// <summary>
    /// True when the caller may read the memory: shared memories are readable by everyone,
    /// a personal memory only by its owner.
    /// </summary>
    /// <param name="memory">The memory to read</param>
    /// <param name="currentUserId">Identifier of the calling user, null when unknown</param>
    public static bool CanRead(AgentMemory memory, Guid? currentUserId) =>
        IsShared(memory) || memory.UserId == currentUserId;

    /// <summary>
    /// True when the caller may change or remove the memory: a shared memory only as administrator,
    /// a personal memory only as its owner.
    /// </summary>
    /// <param name="memory">The memory to change</param>
    /// <param name="currentUserId">Identifier of the calling user, null when unknown</param>
    /// <param name="isAdmin">Whether the calling user holds the administrator role</param>
    public static bool CanWrite(AgentMemory memory, Guid? currentUserId, bool isAdmin) =>
        IsShared(memory) ? isAdmin : memory.UserId == currentUserId;

    /// <summary>
    /// Owner a newly created memory must carry: personal categories belong to their creator,
    /// every other category is shared company-wide knowledge.
    /// </summary>
    /// <param name="category">Category of the new memory</param>
    /// <param name="currentUserId">Identifier of the creating user, null when unknown</param>
    public static Guid? ResolveOwner(string? category, Guid? currentUserId) =>
        MemoryCategories.IsPersonal(category) ? currentUserId : null;

    /// <summary>
    /// True when creating a memory of this category would write shared company-wide knowledge,
    /// which is reserved for administrators.
    /// </summary>
    /// <param name="category">Category of the new memory</param>
    public static bool CreatesSharedMemory(string? category) => !MemoryCategories.IsPersonal(category);
}
