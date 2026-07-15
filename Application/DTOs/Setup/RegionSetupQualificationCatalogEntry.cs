// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One qualification of an industry profile's catalog (K20). Name maps language code to display text;
/// only the core languages (de/en/fr/it) are accepted and at least one non-empty value is required.
/// The first available core-language value (de preferred) — combined with the industry slug — forms the
/// import natural key. IsTimeLimited marks qualifications that expire (K11 checks then apply).
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupQualificationCatalogEntry
{
    public Dictionary<string, string>? Name { get; set; }

    public bool? IsTimeLimited { get; set; }
}
