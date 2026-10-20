// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Geocoding;

/// <summary>
/// The fields of a stored address that the batch geocoding needs to query the geocoder.
/// </summary>
public sealed record AddressWithoutCoordinates(Guid Id, string Street, string Zip, string City, string Country);
