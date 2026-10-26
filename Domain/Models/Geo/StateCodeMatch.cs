// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Geo;

/// <summary>
/// One state (canton, Bundesland, département, ...) a location filter accepts: the code stored on the address and the
/// country the code belongs to. State codes are only unique per country (BE is Bern in CH and Berlin in DE), so a
/// match with a country only accepts addresses of that country (or addresses without any country).
/// </summary>
/// <param name="Code">State code as stored in Address.State (e.g. 'BE')</param>
/// <param name="CountryCode">Country code the state belongs to (e.g. 'CH'); null = any country</param>
public sealed record StateCodeMatch(string Code, string? CountryCode);
