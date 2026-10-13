// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Connects the process to the Windows service control manager as soon as it starts and keeps reporting
/// "start pending" until the web host has started. Klacks migrates the database and initialises plugins, seeds and
/// caches before app.Run(), which takes minutes on a first start or after an update; the stock WindowsServiceLifetime
/// only connects at app.Run(), so the service control manager killed the process after its 30-second limit.
/// A stop request lets the host shut down gracefully before the service is reported as stopped, and the process only
/// exits after that report; otherwise the service control manager would log a crash and restart the service.
/// </summary>

using System.Runtime.Versioning;
using System.ServiceProcess;
using Klacks.Api.Application.Configuration;

namespace Klacks.Api.Infrastructure.Hosting;

[SupportedOSPlatform("windows")]
public sealed class KlacksWindowsService : ServiceBase
{
    private static readonly TimeSpan PendingReportInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PendingWaitHint = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan DispatcherExitWait = TimeSpan.FromSeconds(30);

    private readonly ManualResetEventSlim _applicationStarted = new();
    private readonly ManualResetEventSlim _applicationStopped = new();
    private IHostApplicationLifetime? _lifetime;
    private Thread? _dispatcher;
    private int _stopRequested;

    public KlacksWindowsService()
    {
        ServiceName = NativeHostingOptions.WindowsServiceName;
        CanStop = true;
        CanShutdown = true;
        AutoLog = false;
    }

    public void StartDispatcher()
    {
        _dispatcher = new Thread(() => Run(this)) { IsBackground = true, Name = nameof(KlacksWindowsService) };
        _dispatcher.Start();
    }

    public void CompleteAfterHostExit()
    {
        _applicationStopped.Set();
        if (Volatile.Read(ref _stopRequested) == 0)
        {
            Stop();
        }

        _dispatcher?.Join(DispatcherExitWait);
    }

    public void AttachApplication(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
        lifetime.ApplicationStarted.Register(_applicationStarted.Set);
        lifetime.ApplicationStopped.Register(_applicationStopped.Set);
    }

    protected override void OnStart(string[] args)
    {
        while (!_applicationStarted.Wait(PendingReportInterval))
        {
            RequestAdditionalTime((int)PendingWaitHint.TotalMilliseconds);
        }
    }

    protected override void OnStop()
    {
        RequestHostStop();
        while (!_applicationStopped.Wait(PendingReportInterval))
        {
            RequestAdditionalTime((int)PendingWaitHint.TotalMilliseconds);
        }
    }

    protected override void OnShutdown()
    {
        RequestHostStop();
        _applicationStopped.Wait(ShutdownWait);
    }

    private void RequestHostStop()
    {
        Interlocked.Exchange(ref _stopRequested, 1);
        if (!_applicationStopped.IsSet)
        {
            _lifetime?.StopApplication();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _applicationStarted.Dispose();
            _applicationStopped.Dispose();
        }

        base.Dispose(disposing);
    }
}
