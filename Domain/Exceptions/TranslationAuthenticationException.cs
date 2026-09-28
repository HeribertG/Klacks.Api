// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

public class TranslationAuthenticationException : Exception
{
    public TranslationAuthenticationException(string message) : base(message) { }
    public TranslationAuthenticationException(string message, Exception inner) : base(message, inner) { }
}
