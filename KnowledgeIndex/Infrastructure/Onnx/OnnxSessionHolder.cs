// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Diagnostics;
using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Owns one lazily built, idle-unloadable inference lease on behalf of an ONNX provider: builds it on
/// first use, hands callers a value copy under a lease refcount, and releases it only while nothing
/// runs on it. The providers stay pure inference; every rule about when the native session may be
/// created or freed lives here, once.
/// </summary>
/// <typeparam name="TLease">Everything one inference run needs; disposing it frees the native session.</typeparam>
/// <param name="build">Builds a complete lease or throws. It must leave nothing behind on failure, so
/// that the holder is either fully loaded or holds nothing at all.</param>
/// <param name="maxConcurrentRuns">Upper bound on simultaneous runs; UnlimitedConcurrency disables the gate.</param>
/// <param name="disposeWaitTimeout">How long DisposeAsync waits for running inferences to finish before it
/// gives up and leaves the native session to the process exit; null uses the default.</param>
internal sealed class OnnxSessionHolder<TLease> : IAsyncDisposable where TLease : struct, IDisposable
{
    public const int UnlimitedConcurrency = 0;

    private readonly Func<CancellationToken, Task<TLease>> _build;
    private readonly SemaphoreSlim? _gate;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly TimeSpan _disposeWaitTimeout;

    private TLease? _lease;
    private int _loaded;
    private int _activeRuns;
    private long _lastUsedTimestamp;
    private int _loadCount;
    private int _disposed;

    public OnnxSessionHolder(
        Func<CancellationToken, Task<TLease>> build,
        int maxConcurrentRuns,
        TimeSpan? disposeWaitTimeout = null)
    {
        _build = build;
        _disposeWaitTimeout = disposeWaitTimeout
            ?? TimeSpan.FromMilliseconds(KnowledgeIndexConstants.OnnxDisposeWaitMilliseconds);
        _gate = maxConcurrentRuns > UnlimitedConcurrency
            ? new SemaphoreSlim(maxConcurrentRuns, maxConcurrentRuns)
            : null;
    }

    // Read outside the init lock on purpose: this is only ever an optimization that lets the sweep
    // skip a holder that has nothing, and a stale answer costs at most one wasted try-acquire.
    // Nothing dereferences the lease based on this property.
    public bool IsLoaded => Volatile.Read(ref _loaded) != 0;

    public int LoadCount => Volatile.Read(ref _loadCount);

    /// <summary>
    /// Builds the lease if none is held and hands back a value copy with the run counted as active.
    /// Every successful call must be paired with <see cref="Release"/>.
    /// </summary>
    public async Task<TLease> AcquireAsync(CancellationToken ct)
    {
        // No lock-free fast path here on purpose. TryUnloadIfIdleAsync can dispose the lease, and a
        // native InferenceSession freed under a running Run() faults the process rather than throwing.
        // Reading the lease under the lock and handing the caller a value copy is what makes an unload
        // during inference structurally impossible. The uncontended acquire costs ~100 ns against an
        // inference measured in tens of milliseconds - do not reintroduce the shortcut.
        await _initLock.WaitAsync(ct);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

            if (_lease is null)
            {
                // Assigned only once the build has returned: a builder that throws halfway leaves the
                // holder exactly as it was, so the next call retries from scratch instead of
                // dereferencing a half-built lease.
                _lease = await _build(ct);

                // Stamped here rather than only on release: a lease that has been built but has not
                // yet finished a call would otherwise carry timestamp 0, and Stopwatch.GetElapsedTime(0)
                // measures time since boot - the sweep would read a brand new session as infinitely idle.
                Volatile.Write(ref _lastUsedTimestamp, Stopwatch.GetTimestamp());
                Interlocked.Increment(ref _loadCount);
                Volatile.Write(ref _loaded, 1);
            }

            Interlocked.Increment(ref _activeRuns);
            return _lease.Value;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // Timestamp is written BEFORE the counter drops: the unloader only looks at the timestamp once it
    // has seen the counter at zero, so this ordering can only ever make a session look busier, never
    // idler, than it is.
    public void Release()
    {
        Volatile.Write(ref _lastUsedTimestamp, Stopwatch.GetTimestamp());
        Interlocked.Decrement(ref _activeRuns);
    }

    // Lock order invariant for callers: lease first, gate second. The reverse would hold the only gate
    // slot while a cold session load reads hundreds of megabytes off disk.
    public async Task WaitForSlotAsync(CancellationToken ct)
    {
        if (_gate is not null)
        {
            await _gate.WaitAsync(ct);
        }
    }

    public void ReleaseSlot()
    {
        _gate?.Release();
    }

    public async Task<bool> TryUnloadIfIdleAsync(TimeSpan idleFor, CancellationToken cancellationToken)
    {
        // Try-acquire, never block: whoever is building or tearing down the lease wins, and the sweep
        // simply looks again on its next tick.
        if (!await _initLock.WaitAsync(0, cancellationToken))
        {
            return false;
        }

        try
        {
            if (_lease is null) return false;
            if (Volatile.Read(ref _activeRuns) > 0) return false;
            if (Stopwatch.GetElapsedTime(Volatile.Read(ref _lastUsedTimestamp)) < idleFor) return false;

            DisposeLeaseUnderLock();
            return true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // The container hands one provider out under several service types and disposes each of
        // them, so this method runs more than once. Waiting on a disposed SemaphoreSlim throws
        // ObjectDisposedException, so the second entry has to turn back here.
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Taken under the init lock, unlike a plain dispose: it closes the shutdown window in which the
        // idle sweep is mid-unload or a call still holds a lease. Freeing a native InferenceSession
        // while Run() executes on it faults the process instead of throwing.
        // A run holds a value copy of the lease and does not take the init lock, so the lock alone does
        // not keep it from finishing on a freed session: shutdown also waits for every active run (a
        // background index sync can be mid-inference when the container is disposed). If they do not
        // drain in time the session and the gate are left undisposed - the process is exiting, and a
        // leak is harmless where a freed session under Run() faults natively.
        bool drained;
        await _initLock.WaitAsync();
        try
        {
            drained = await WaitForActiveRunsToDrainAsync();
            if (drained)
            {
                DisposeLeaseUnderLock();
            }
        }
        finally
        {
            _initLock.Release();
        }

        if (drained)
        {
            _gate?.Dispose();
        }

        _initLock.Dispose();
    }

    private async Task<bool> WaitForActiveRunsToDrainAsync()
    {
        var started = Stopwatch.GetTimestamp();
        while (Volatile.Read(ref _activeRuns) > 0)
        {
            if (Stopwatch.GetElapsedTime(started) >= _disposeWaitTimeout)
            {
                return false;
            }

            await Task.Delay(KnowledgeIndexConstants.OnnxDisposePollMilliseconds);
        }

        return true;
    }

    private void DisposeLeaseUnderLock()
    {
        if (_lease is null) return;

        Volatile.Write(ref _loaded, 0);
        _lease.Value.Dispose();
        _lease = null;
    }
}
