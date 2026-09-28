// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// Thrown when a container modification is attempted without holding a matching edit lock.
/// </summary>
public class ContainerLockedException : Exception
{
    public ContainerLockedException(string message)
        : base(message)
    {
    }
}
