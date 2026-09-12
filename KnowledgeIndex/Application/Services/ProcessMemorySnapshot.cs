// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// One reading of how much memory this process holds, split into the resident total and the managed
/// part of it. The ONNX model weights are native allocations the garbage collector never sees, so a
/// resident number on its own cannot say whether a rise came from the inference sessions or from
/// ordinary managed work. Subtracting the managed heap from the resident total is what makes that
/// decision possible, and it is why both ONNX log lines carry all three values.
/// Construction from raw byte counts is public so the unit conversion can be verified without
/// touching the live process; <see cref="Capture"/> is the only member that reads it.
/// </summary>
/// <param name="residentBytes">Working set: everything the operating system currently keeps in RAM for this process, managed and native alike.</param>
/// <param name="managedHeapSizeBytes">Managed heap size as the collector last measured it, including the parts it has not returned to the allocator.</param>
/// <param name="allocatedManagedBytes">Managed bytes currently considered allocated; below the heap size by whatever the collector holds in reserve.</param>
public sealed class ProcessMemorySnapshot
{
    private const double BytesPerMegabyte = 1024d * 1024d;

    public ProcessMemorySnapshot(long residentBytes, long managedHeapSizeBytes, long allocatedManagedBytes)
    {
        ResidentMegabytes = ToMegabytes(residentBytes);
        ManagedHeapMegabytes = ToMegabytes(managedHeapSizeBytes);
        AllocatedManagedMegabytes = ToMegabytes(allocatedManagedBytes);
    }

    public double ResidentMegabytes { get; }

    public double ManagedHeapMegabytes { get; }

    public double AllocatedManagedMegabytes { get; }

    /// <summary>
    /// The share of resident memory the managed heap does not account for: the runtime itself, loaded
    /// images, and the native ONNX session weights this whole reading exists for. It can go negative
    /// when parts of the managed heap are paged out, and is reported as measured rather than clamped,
    /// because a negative value is a signal about the host, not a rounding artefact.
    /// </summary>
    public double NativeMegabytes => ResidentMegabytes - ManagedHeapMegabytes;

    public static ProcessMemorySnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();

        return new ProcessMemorySnapshot(
            process.WorkingSet64,
            GC.GetGCMemoryInfo().HeapSizeBytes,
            GC.GetTotalMemory(false));
    }

    private static double ToMegabytes(long bytes) => bytes / BytesPerMegabyte;
}
