// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// ONNX Runtime configuration keys and values used by the knowledge-index providers. Mirrors the
/// string constants in onnxruntime's session/run options config headers so no call site spells them.
/// </summary>
public static class OnnxRuntimeConfigKeys
{
    // Run option: after each Run() the arena releases every fully free region back to the device
    // allocator. Value lists the devices to shrink, "cpu:0" for the CPU arena.
    public const string RunEnableMemoryArenaShrinkage = "memory.enable_memory_arena_shrinkage";

    public const string CpuDeviceZero = "cpu:0";

    // Session option: use the allocators registered on OrtEnv (OrtEnv.CreateAndRegisterAllocator)
    // instead of a session-private arena. Required for an arena with a memory cap or a custom
    // extend strategy, since SessionOptions exposes neither.
    public const string SessionUseEnvAllocators = "session.use_env_allocators";

    public const string Enabled = "1";

    // OrtArenaCfg.arenaExtendStrategy values (onnxruntime ArenaExtendStrategy enum).
    public const int ArenaExtendNextPowerOfTwo = 0;
    public const int ArenaExtendSameAsRequested = 1;

    // OrtArenaCfg: 0 for maxMemory means "no cap"; -1 for the chunk parameters means "runtime default".
    public const uint ArenaNoMemoryCap = 0;
    public const int ArenaRuntimeDefault = -1;
}
