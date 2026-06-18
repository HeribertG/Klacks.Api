// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.Services;

/// <summary>
/// Singleton <see cref="SemaphoreSlim"/>(1,1) implementation of <see cref="IHeavyWorkGate"/>. One
/// memory-heavy background job runs at a time; everyone else waits or skips. Disposing the lease
/// releases the gate exactly once (double-dispose is a no-op).
/// </summary>
public sealed class HeavyWorkGate : IHeavyWorkGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        return new Lease(_semaphore);
    }

    public bool TryAcquire(out IDisposable? lease)
    {
        if (_semaphore.Wait(0))
        {
            lease = new Lease(_semaphore);
            return true;
        }

        lease = null;
        return false;
    }

    private sealed class Lease : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Lease(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _semaphore, null);
            semaphore?.Release();
        }
    }
}
