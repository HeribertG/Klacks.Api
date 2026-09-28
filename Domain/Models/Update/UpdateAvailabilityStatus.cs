// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Update;

public enum UpdateAvailabilityStatus
{
    UpToDate = 0,
    UpdateAvailable = 1,
    UpdateRequiresIntermediate = 2,
    ManifestInvalid = 3,
}
