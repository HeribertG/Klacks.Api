// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// An expense refused because its work is a service inside a container (Owner 2026-11-04: expenses belong to the
/// container only), answered with 400 and the code <see cref="Code"/>.
/// </summary>
public sealed class ContainerServiceExpenseRefusedException : InvalidRequestException, ICodedRejection
{
    public const string Code = "EXPENSE_ON_CONTAINER_SERVICE";

    private const string RefusalMessage =
        "Expenses can only be recorded on the container, not on a service inside it.";

    public ContainerServiceExpenseRefusedException()
        : base(RefusalMessage)
    {
    }

    public string ErrorCode => Code;

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}
