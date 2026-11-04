// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A container or container template save refused because a service, an absence or a travel/briefing time lies outside
/// the container, answered with 400 and the code ContainerFitRefusal.Code.
/// </summary>
/// <param name="container">The container span (HH:mm-HH:mm)</param>
/// <param name="outside">The spans outside the container, comma separated</param>
public sealed class ContainerFitRefusedException : InvalidRequestException, ICodedRejection
{
    public ContainerFitRefusedException(string container, string outside)
        : base(string.Format(ContainerFitRefusal.MessageFormat, container, outside))
    {
        Details = new Dictionary<string, object?>
        {
            [ContainerFitRefusal.ContainerField] = container,
            [ContainerFitRefusal.OutsideField] = outside,
        };
    }

    public string ErrorCode => ContainerFitRefusal.Code;

    public IReadOnlyDictionary<string, object?> Details { get; }
}
