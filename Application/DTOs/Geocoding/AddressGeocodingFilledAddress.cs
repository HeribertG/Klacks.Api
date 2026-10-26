// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingFilledAddress(
    AddressGeocodingCandidate Address,
    double Latitude,
    double Longitude,
    AddressGeocodingSource Source);
