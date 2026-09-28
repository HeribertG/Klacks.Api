// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

public enum UpdateOperationCancellationOutcome
{
    Cancelled,
    NotFound,
    NoLongerPending,
}
