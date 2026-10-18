// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A rejection that carries a stable machine code and detail fields next to its English message. ErrorHandlingMiddleware
/// writes both into the problem body, so a client can explain the rejection in the user's language without parsing text.
/// </summary>
public interface ICodedRejection
{
    string ErrorCode { get; }

    IReadOnlyDictionary<string, object?> Details { get; }
}
