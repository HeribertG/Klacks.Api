// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the industry slugs activated via the ACTIVE_INDUSTRIES setting for selection-list
/// filtering. Null means the setting is missing or blank: all industries are active and no
/// filtering applies. An empty (non-null) collection means the setting holds the "custom" marker:
/// no shipped industry profile is active, only industry-less rows are selectable.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IActiveIndustriesProvider
{
    Task<IReadOnlyCollection<string>?> GetActiveIndustrySlugsAsync();
}
