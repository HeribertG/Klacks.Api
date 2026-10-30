// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits of the container bundling advice. At most <see cref="MaxGeocodedAddresses"/> addresses without stored
/// coordinates go to the geocoder per question, because it answers about one address per second and the advice runs inside
/// a chat turn; at most <see cref="MaxListedLeftOut"/> services are listed by name, the count stays complete. A tour needs
/// <see cref="MinVisitsForABundle"/> visits to be a bundle at all.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class ContainerBundlingAdviceLimits
{
    public const int MaxGeocodedAddresses = 25;

    public const int MaxListedLeftOut = 20;

    public const int MinVisitsForABundle = 2;
}
