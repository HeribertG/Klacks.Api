// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Average position of every geocoded address that carries the same city name. Used as the location
/// of a city group that has no coordinates of its own.
/// </summary>
/// <param name="City">City name as stored on the addresses, trimmed and lower-cased.</param>
/// <param name="Latitude">Mean latitude of those addresses.</param>
/// <param name="Longitude">Mean longitude of those addresses.</param>
/// <param name="AddressCount">Number of addresses the mean was built from; weights merged centroids.</param>
public sealed record CityCentroid(string City, double Latitude, double Longitude, int AddressCount);
