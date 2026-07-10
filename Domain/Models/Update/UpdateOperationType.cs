// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Update;

public enum UpdateOperationType
{
    Update = 0,
    Rollback = 1,
    WhisperInstall = 2,
    WhisperUninstall = 3,
}
