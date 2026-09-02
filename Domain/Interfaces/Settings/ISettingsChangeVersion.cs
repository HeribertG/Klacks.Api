// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Settings;

/// <summary>
/// Process-wide counter that advances every time a settings row is persisted, so long-lived caches
/// of settings-derived data can tell whether they were built before or after the latest write.
/// </summary>
public interface ISettingsChangeVersion
{
    /// <summary>
    /// The current version. Two reads returning the same value guarantee no settings write was
    /// persisted in between; a higher value means at least one write happened since an earlier read.
    /// </summary>
    long Current { get; }

    /// <summary>
    /// Advances the version. Callers persist the settings write first, then call this.
    /// </summary>
    void Bump();
}
