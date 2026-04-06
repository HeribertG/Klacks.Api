// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
