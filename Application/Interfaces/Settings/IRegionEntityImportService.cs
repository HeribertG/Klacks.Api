// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies ONLY the re-importable parts of a parsed region setup profile: industry-profile entity
/// imports (scheduling rule presets incl. rate revisions, qualification catalogs, industry-bound
/// compliance rows), macro imports and the package identity settings. Never touches the once-only
/// settings sections or the ACTIVE_INDUSTRIES setting.
/// </summary>
using Klacks.Api.Application.DTOs.Setup;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface IRegionEntityImportService
{
    Task ApplyEntityImportsAsync(RegionSetupProfile profile);
}
