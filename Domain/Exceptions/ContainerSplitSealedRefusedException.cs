// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A split refused because the container is sealed (confirmed, approved or closed): a sealed container is not split; once
/// unsealed and split, each part is sealed on its own (Owner 2026-11-04). Answered with 400 and the code <see cref="Code"/>.
/// </summary>
public sealed class ContainerSplitSealedRefusedException : InvalidRequestException, ICodedRejection
{
    public const string Code = "CONTAINER_SPLIT_SEALED_REFUSED";

    private const string RefusalMessage =
        "A sealed container cannot be split. Remove the seal first; afterwards each part can be sealed on its own.";

    public ContainerSplitSealedRefusedException()
        : base(RefusalMessage)
    {
    }

    public string ErrorCode => Code;

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}
