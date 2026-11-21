// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Update;

public enum UpdaterCapabilityState
{
    Missing = 0,
    Stale = 1,
    Supported = 2,
}
