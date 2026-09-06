// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

/// <summary>
/// Asks the process allocator to return freed pages to the operating system. Disposing a native
/// inference session hands its buffers back to the allocator, which is free to keep them on its own
/// heap; this is the request to give them up.
/// </summary>
public interface IProcessHeapTrimmer
{
    /// <summary>Returns whether anything was actually released; false also on platforms without a
    /// trimmable allocator, where the call is a no-op.</summary>
    bool TryTrim();
}
