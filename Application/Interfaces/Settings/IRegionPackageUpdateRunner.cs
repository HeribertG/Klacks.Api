// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Executes a single marketplace region-package update check cycle for the installed package.
/// </summary>
namespace Klacks.Api.Application.Interfaces.Settings;

public interface IRegionPackageUpdateRunner
{
    Task RunCycleAsync(CancellationToken cancellationToken);
}
