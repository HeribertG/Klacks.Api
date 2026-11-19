// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// The directory server could not be reached or refused the request. Carries a fixed English message; the
/// directory's own error stays in the inner exception for the log.
/// </summary>
/// <param name="innerException">The LDAP error that caused it</param>
public sealed class DirectoryUnavailableException : Exception
{
    public const string DefaultMessage = "The directory could not be read.";

    public DirectoryUnavailableException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
