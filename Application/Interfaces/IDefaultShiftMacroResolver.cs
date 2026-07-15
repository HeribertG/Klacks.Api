// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Resolves the automatic default calculation macro applied to a new shift when the caller did
/// not supply one explicitly.
/// </summary>
public interface IDefaultShiftMacroResolver
{
    /// <summary>
    /// Returns the id of the active macro marked as the default for category Shift, or null when
    /// none is configured. Never throws — a missing default is a valid, opt-out-friendly state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the underlying macro lookup.</param>
    Task<Guid?> ResolveDefaultMacroIdAsync(CancellationToken cancellationToken = default);
}
