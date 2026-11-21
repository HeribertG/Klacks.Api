// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Read-only decision whether a PostgreSQL engine upgrade could be queued right now. The status card and the engine upgrade
/// command share it, so the card never offers what the command refuses.
/// </summary>
public interface IEngineUpgradePreflight
{
    /// <summary>
    /// Reads the manifest of the configured channel itself and decides.
    /// </summary>
    Task<EngineUpgradePreflightResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Decides against a manifest the caller already read; null means no manifest could be read.
    /// </summary>
    Task<EngineUpgradePreflightResult> CheckAsync(UpdateManifest? manifest, CancellationToken cancellationToken = default);
}
