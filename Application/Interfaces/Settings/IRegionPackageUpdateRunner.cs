// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes a single marketplace region-package update check cycle for the installed package.
/// </summary>
namespace Klacks.Api.Application.Interfaces.Settings;

public interface IRegionPackageUpdateRunner
{
    Task RunCycleAsync(CancellationToken cancellationToken);
}
