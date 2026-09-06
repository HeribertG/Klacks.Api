// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;

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
internal sealed class OnnxSessionHolder<TLease> : IAsyncDisposable where TLease : struct, IDisposable
{
    public const int UnlimitedConcurrency = 0;

    private readonly Func<CancellationToken, Task<TLease>> _build;
    private readonly SemaphoreSlim? _gate;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private TLease? _lease;
    private int _loaded;
    private int _activeRuns;
    private long _lastUsedTimestamp;
    private int _loadCount;
    private int _disposed;

    public OnnxSessionHolder(Func<CancellationToken, Task<TLease>> build, int maxConcurrentRuns)
    {
        _build = build;
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
        await _initLock.WaitAsync();
        try
        {
            DisposeLeaseUnderLock();
        }
        finally
        {
            _initLock.Release();
        }

        _gate?.Dispose();
        _initLock.Dispose();
    }

    private void DisposeLeaseUnderLock()
    {
        if (_lease is null) return;

        Volatile.Write(ref _loaded, 0);
        _lease.Value.Dispose();
        _lease = null;
    }
}
