// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Releases idle ONNX inference sessions so a container that spends most of its day waiting does not
/// hold ~1.1 GB of model weights the whole time. Sessions rebuild themselves on the next call, so the
/// only cost of a wrong guess here is one slower request, never a wrong answer.
/// Deliberately a single sweep over every unloadable session rather than a timer inside each provider:
/// the idle window is policy and belongs next to the configuration, while the providers stay pure
/// inference. It is also the only place that can measure resident memory on both sides of the dispose,
/// which is the number this whole feature is judged by.
/// </summary>
/// <param name="sessions">Every provider that can hand its session back; empty on hosts without ONNX.</param>
/// <param name="configuration">Supplies the idle window in minutes; zero disables the sweep.</param>
/// <param name="heapTrimmer">Asks the allocator for the pages the session dispose only handed back to it.</param>
/// <param name="timeProvider">The production clock behind the poll timer.</param>
/// <param name="logger">Reports each release with the resident memory measured on both sides of it.</param>
public sealed class OnnxSessionIdleUnloadService : BackgroundService
{
    private readonly IReadOnlyList<IUnloadableInferenceSession> _sessions;
    private readonly TimeSpan _idleFor;
    private readonly IProcessHeapTrimmer _heapTrimmer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OnnxSessionIdleUnloadService> _logger;

    public OnnxSessionIdleUnloadService(
        IEnumerable<IUnloadableInferenceSession> sessions,
        IConfiguration configuration,
        IProcessHeapTrimmer heapTrimmer,
        TimeProvider timeProvider,
        ILogger<OnnxSessionIdleUnloadService> logger)
    {
        _sessions = sessions.ToList();
        _heapTrimmer = heapTrimmer;
        _timeProvider = timeProvider;
        _logger = logger;
        _idleFor = ResolveIdleWindow(configuration, logger);
    }

    // Zero - and any negative value, which can only be a mistake - means off. Everything else is held
    // at or above MinimumIdleUnloadMinutes: see the constant for why a one-minute window is a lever an
    // ordinary user can pull against every other user.
    private static TimeSpan ResolveIdleWindow(
        IConfiguration configuration, ILogger<OnnxSessionIdleUnloadService> logger)
    {
        var configuredMinutes = configuration.GetValue(
            KnowledgeIndexConstants.IdleUnloadMinutesConfigKey,
            KnowledgeIndexConstants.DefaultIdleUnloadMinutes);

        if (configuredMinutes <= KnowledgeIndexConstants.IdleUnloadDisabled)
        {
            return TimeSpan.Zero;
        }

        if (configuredMinutes < KnowledgeIndexConstants.MinimumIdleUnloadMinutes)
        {
            logger.LogWarning(
                "{ConfigKey} is {ConfiguredMinutes} min, below the supported minimum; raised to "
                + "{MinimumMinutes} min. Set it to {DisabledValue} to switch idle unloading off.",
                KnowledgeIndexConstants.IdleUnloadMinutesConfigKey,
                configuredMinutes,
                KnowledgeIndexConstants.MinimumIdleUnloadMinutes,
                KnowledgeIndexConstants.IdleUnloadDisabled);

            return TimeSpan.FromMinutes(KnowledgeIndexConstants.MinimumIdleUnloadMinutes);
        }

        return TimeSpan.FromMinutes(configuredMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_idleFor <= TimeSpan.Zero)
        {
            _logger.LogInformation(
                "ONNX idle unload disabled ({ConfigKey} is {Minutes}); the inference sessions stay resident.",
                KnowledgeIndexConstants.IdleUnloadMinutesConfigKey,
                KnowledgeIndexConstants.IdleUnloadDisabled);
            return;
        }

        if (_sessions.Count == 0)
        {
            _logger.LogInformation(
                "ONNX idle unload has nothing to watch; this host runs no unloadable inference session.");
            return;
        }

        _logger.LogInformation(
            "ONNX idle unload active: {Count} session(s) are released after {IdleMinutes} min without a call.",
            _sessions.Count,
            _idleFor.TotalMinutes);

        // The injected TimeProvider is the production clock, nothing more. Driving this loop from a test
        // would need a provider that also overrides CreateTimer, and the repository's SettableTimeProvider
        // only overrides GetUtcNow - so the loop itself is untested. What the tests do cover is the body:
        // SweepAsync is internal and called directly, which is where the whole decision lives.
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(KnowledgeIndexConstants.IdleUnloadPollSeconds), _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SweepAsync(stoppingToken);
        }
    }

    // internal so a unit test can run one tick's worth of work directly instead of waiting a real minute
    // for the timer (InternalsVisibleTo for Klacks.UnitTest is set up project-wide).
    internal async Task SweepAsync(CancellationToken cancellationToken)
    {
        foreach (var session in _sessions)
        {
            if (!session.IsLoaded)
            {
                continue;
            }

            try
            {
                var before = ProcessMemorySnapshot.Capture();
                if (!await session.TryUnloadIfIdleAsync(_idleFor, cancellationToken))
                {
                    continue;
                }

                var afterUnload = ProcessMemorySnapshot.Capture();

                // Disposing the session only hands its buffers back to the allocator, which keeps most
                // of them. Measured on Linux/glibc 2026-09-05: the dispose alone returned 35-50% of the
                // embedding session's resident memory, and the trim recovered a further 413 MB for 71%
                // in total; the reranker went from 31% to 40%. That gap is the reason this call exists,
                // and it is why the log carries all three numbers rather than only the dispose delta.
                var released = _heapTrimmer.TryTrim();
                var afterTrim = ProcessMemorySnapshot.Capture();

                // The managed numbers are here to keep this line interpretable on its own: a release
                // that shows up in the resident total but not in the managed heap is the native model
                // weights going away, which is what the unload is supposed to achieve. Managed churn
                // moving instead means the measurement caught a collection, not the unload.
                _logger.LogInformation(
                    "ONNX idle unload: {Session} released after {IdleMinutes} min without a call; " +
                    "resident memory {BeforeMb:F0} -> {AfterUnloadMb:F0} -> {AfterTrimMb:F0} MB " +
                    "(freed {FreedMb:F0} MB, heap trim released pages: {HeapTrimReleased}), " +
                    "managed heap {BeforeHeapMb:F0} -> {AfterTrimHeapMb:F0} MB, " +
                    "managed allocated {BeforeAllocatedMb:F0} -> {AfterTrimAllocatedMb:F0} MB, " +
                    "native share {BeforeNativeMb:F0} -> {AfterTrimNativeMb:F0} MB, " +
                    "load count {LoadCount}.",
                    session.SessionName,
                    _idleFor.TotalMinutes,
                    before.ResidentMegabytes,
                    afterUnload.ResidentMegabytes,
                    afterTrim.ResidentMegabytes,
                    before.ResidentMegabytes - afterTrim.ResidentMegabytes,
                    released,
                    before.ManagedHeapMegabytes,
                    afterTrim.ManagedHeapMegabytes,
                    before.AllocatedManagedMegabytes,
                    afterTrim.AllocatedManagedMegabytes,
                    before.NativeMegabytes,
                    afterTrim.NativeMegabytes,
                    session.LoadCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One provider failing to release must not stop the other from trying: the whole
                // point is to free memory, and half of it is still worth having.
                _logger.LogWarning(ex, "ONNX idle unload failed for {Session}.", session.SessionName);
            }
        }
    }
}
