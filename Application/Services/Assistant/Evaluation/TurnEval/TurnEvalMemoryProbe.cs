// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Optional diagnostic that logs one memory line every N replayed items of a turn-eval run, so growth of the
/// test host can be attributed to the managed heap or to native memory. The line is written in a fixed order:
/// first the state as it is (no forced collection), then one forced full collection, then the values after it.
/// The forced collection pauses the run and therefore slightly changes its behaviour; the probe is off unless
/// the environment variable named by TurnEvalDefaults.MemoryProbeEnvironmentVariable is set to
/// TurnEvalDefaults.MemoryProbeEnabledValue.
/// </summary>
/// <param name="enabled">Whether the probe writes anything at all</param>
/// <param name="everyItems">Probe interval in items; the first item is always probed as well</param>

using System.Diagnostics;
using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.KnowledgeIndex.Application.Services;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public sealed class TurnEvalMemoryProbe(bool enabled, int everyItems)
{
    private const double BytesPerMegabyte = 1024d * 1024d;

    public bool Enabled { get; } = enabled;

    public int EveryItems { get; } = everyItems;

    public static TurnEvalMemoryProbe FromEnvironment() =>
        new(
            string.Equals(
                Environment.GetEnvironmentVariable(TurnEvalDefaults.MemoryProbeEnvironmentVariable),
                TurnEvalDefaults.MemoryProbeEnabledValue,
                StringComparison.Ordinal),
            TurnEvalDefaults.MemoryProbeEveryItems);

    public bool ShouldProbe(int itemNumber) =>
        Enabled && EveryItems > 0 && (itemNumber == 1 || itemNumber % EveryItems == 0);

    public void Write(ILogger logger, int itemNumber)
    {
        var before = ProcessMemorySnapshot.Capture();
        var (beforePrivateMb, beforeThreads, beforeHandles) = ReadProcessResources();
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);

        var retainedManagedBytes = GC.GetTotalMemory(forceFullCollection: true);
        var gcInfo = GC.GetGCMemoryInfo();
        var after = ProcessMemorySnapshot.Capture();
        var (afterPrivateMb, afterThreads, afterHandles) = ReadProcessResources();
        var largeObjectHeapBytes = gcInfo.GenerationInfo.Length > TurnEvalDefaults.LargeObjectHeapGenerationIndex
            ? gcInfo.GenerationInfo[TurnEvalDefaults.LargeObjectHeapGenerationIndex].SizeAfterBytes
            : 0;

        logger.LogInformation(
            TurnEvalDefaults.MemoryProbeLogTemplate,
            itemNumber,
            before.ResidentMegabytes, before.ManagedHeapMegabytes, before.NativeMegabytes,
            before.AllocatedManagedMegabytes,
            beforePrivateMb, beforeThreads, beforeHandles,
            retainedManagedBytes / BytesPerMegabyte,
            gcInfo.HeapSizeBytes / BytesPerMegabyte,
            gcInfo.FragmentedBytes / BytesPerMegabyte,
            gcInfo.TotalCommittedBytes / BytesPerMegabyte,
            largeObjectHeapBytes / BytesPerMegabyte,
            after.ResidentMegabytes, after.NativeMegabytes,
            afterPrivateMb, afterThreads, afterHandles,
            gen0, gen1, gen2);
    }

    private static (double PrivateMegabytes, int Threads, int Handles) ReadProcessResources()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return (process.PrivateMemorySize64 / BytesPerMegabyte, process.Threads.Count, process.HandleCount);
    }
}
