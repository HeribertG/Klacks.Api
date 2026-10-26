// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum AddressGeocodingFillStatus
{
    Filled = 1,
    NotFound = 2,
    Unavailable = 3,
    Disabled = 4,
    Superseded = 5
}
