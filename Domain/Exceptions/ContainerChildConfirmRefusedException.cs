// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A confirmation refused because its work is a service inside a container: the container confirms it together with its
/// children (Owner 2026-11-04). Answered with 400 and the code <see cref="Code"/>.
/// </summary>
public sealed class ContainerChildConfirmRefusedException : InvalidRequestException, ICodedRejection
{
    public const string Code = "CONTAINER_CHILD_CONFIRM_REFUSED";

    private const string RefusalMessage =
        "A service inside a container is confirmed together with its container, not on its own.";

    public ContainerChildConfirmRefusedException()
        : base(RefusalMessage)
    {
    }

    public string ErrorCode => Code;

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}
