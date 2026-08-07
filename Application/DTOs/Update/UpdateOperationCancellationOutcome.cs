// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Update;

public enum UpdateOperationCancellationOutcome
{
    Cancelled,
    NotFound,
    NoLongerPending,
}
