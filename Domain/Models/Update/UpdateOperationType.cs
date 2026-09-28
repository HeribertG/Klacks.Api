// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Update;

public enum UpdateOperationType
{
    Update = 0,
    Rollback = 1,
    WhisperInstall = 2,
    WhisperUninstall = 3,
}
