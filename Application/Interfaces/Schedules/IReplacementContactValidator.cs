// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates a contact attempt before it is written to the replacement request book: every referenced entity
/// must exist and be visible to the caller. Anything missing or hidden is answered with KeyNotFoundException
/// (one message per kind, never revealing whether a hidden entity exists); implausible values with
/// InvalidRequestException.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IReplacementContactValidator
{
    /// <param name="request">The contact attempt as posted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ValidateAsync(RecordReplacementContactRequest request, CancellationToken cancellationToken = default);
}
