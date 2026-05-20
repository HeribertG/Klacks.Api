// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Translation;

public class TranslationAuthenticationException : Exception
{
    public TranslationAuthenticationException(string message) : base(message) { }
    public TranslationAuthenticationException(string message, Exception inner) : base(message, inner) { }
}
