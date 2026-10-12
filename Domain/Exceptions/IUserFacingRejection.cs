// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// Marks an exception that rejects a request for a business, validation, lookup, conflict or rights reason and whose
/// message is written for the caller: ErrorHandlingMiddleware returns it verbatim with a 4xx status. The skill executor
/// relies on the same contract and answers such an exception with a plain error result instead of an execution error.
/// Only mark a type whose message, and the message of every subclass, is safe to show to the user.
/// </summary>
public interface IUserFacingRejection
{
}
