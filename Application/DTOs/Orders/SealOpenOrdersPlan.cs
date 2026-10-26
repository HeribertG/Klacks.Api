// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Orders;

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// A seal_open_orders call resolved into its dry-run: either the refusal the caller gets, or the command (Apply=false) with
/// the scope it carries, the group note naming the resolved group and the preview of what an apply would do right now.
/// </summary>
/// <param name="Refusal">Model-facing reason the call cannot run; null when the call was resolved.</param>
/// <param name="Command">The resolved command with Apply=false; null on a refusal.</param>
/// <param name="Preview">The dry-run result of Command, including the full selection; null on a refusal.</param>
/// <param name="GroupNote">"Group: 'X'. " when a group was named, empty otherwise.</param>
public sealed record SealOpenOrdersPlan(
    string? Refusal,
    SealOpenOrdersCommand? Command,
    SealOpenOrdersResult? Preview,
    string GroupNote)
{
    public bool IsRefused => Refusal != null;

    public static SealOpenOrdersPlan Refuse(string refusal) => new(refusal, null, null, string.Empty);
}
