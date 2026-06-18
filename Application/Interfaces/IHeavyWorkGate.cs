// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Process-wide mutual exclusion for memory-heavy background work so two such jobs never peak
/// simultaneously inside the container memory limit (the OOM scar: the ONNX knowledge-index embedding
/// was OOM-killed under the 2 GiB cgroup). The Wizard-4 optimiser and the embedding/index work should
/// both acquire this gate. Acquire returns a lease; dispose it to release.
/// </summary>
public interface IHeavyWorkGate
{
    /// <summary>Waits until the gate is free, then holds it until the returned lease is disposed.</summary>
    Task<IDisposable> AcquireAsync(CancellationToken ct);

    /// <summary>Non-blocking acquire: returns true with a lease when the gate was free, false when busy (caller should skip and retry later).</summary>
    bool TryAcquire(out IDisposable? lease);
}
