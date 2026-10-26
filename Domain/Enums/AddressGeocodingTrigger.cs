// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum AddressGeocodingTrigger
{
    SystemScan = 1,
    Assistant = 2,
    Import = 3,
    AdminBatch = 4,
    Autofill = 5
}
