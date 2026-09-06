// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Runtime.InteropServices;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Heap trimmer backed by glibc's malloc_trim, which walks the allocator's free lists and returns the
/// pages it can to the kernel. Linux/glibc only; every other platform gets a no-op that reports false.
/// </summary>
public sealed class GlibcHeapTrimmer : IProcessHeapTrimmer
{
    private const string LibcName = "libc";

    // Bytes of headroom malloc_trim may keep at the top of the heap. Zero asks for everything.
    private const nuint TrimPadBytes = 0;

    // malloc_trim returns 1 when it was able to release memory and 0 when it was not.
    private const int MemoryWasReleased = 1;

    [DllImport(LibcName, EntryPoint = "malloc_trim")]
    private static extern int MallocTrim(nuint pad);

    public bool TryTrim()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return MallocTrim(TrimPadBytes) == MemoryWasReleased;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
