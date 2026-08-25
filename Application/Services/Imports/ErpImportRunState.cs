// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Singleton flag tracking whether the ERP order import is currently running. IErpOrderImportRunner
/// is scoped and resolved fresh per background-service tick, so a field on the runner itself cannot
/// answer "is an import going on right now" for another consumer (e.g. QuietWindowService) - this is
/// the one piece of state that survives across those scopes, updated by the runner around its actual
/// import work.
/// </summary>

namespace Klacks.Api.Application.Services.Imports;

public sealed class ErpImportRunState
{
    private int _isRunning;

    public bool IsRunning => Volatile.Read(ref _isRunning) != 0;

    public void MarkStarted() => Interlocked.Exchange(ref _isRunning, 1);

    public void MarkFinished() => Interlocked.Exchange(ref _isRunning, 0);
}
