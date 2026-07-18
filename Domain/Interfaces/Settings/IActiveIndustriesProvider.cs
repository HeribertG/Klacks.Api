// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the industry slugs activated via the ACTIVE_INDUSTRIES setting for selection-list
/// filtering. Null means the setting is missing or blank: all industries are active and no
/// filtering applies.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IActiveIndustriesProvider
{
    Task<IReadOnlyCollection<string>?> GetActiveIndustrySlugsAsync();
}
