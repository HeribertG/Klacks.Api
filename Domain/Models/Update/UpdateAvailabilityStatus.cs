// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Update;

public enum UpdateAvailabilityStatus
{
    UpToDate = 0,
    UpdateAvailable = 1,
    UpdateRequiresIntermediate = 2,
    ManifestInvalid = 3,
}
