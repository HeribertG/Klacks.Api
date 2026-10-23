// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The confirmation binding of seal_open_orders: every order its preview matched, each tagged with how the preview
/// classified it ("sealable:" or "blocked:" followed by the id in N format), so an order that appeared, disappeared or
/// changed its classification between the preview and the user's yes makes the confirmation void.
/// </summary>

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Orders;

public static class SealOpenOrdersBinding
{
    private const string GuidFormat = "N";
    private const string SealablePrefix = "sealable:";
    private const string BlockedPrefix = "blocked:";

    public static ConfirmationBinding Of(OpenOrderSealingSelection selection) =>
        ConfirmationBinding.Of(
            ConfirmationBindingKinds.OpenOrdersSeal,
            selection.SealableIds.Select(id => SealablePrefix + id.ToString(GuidFormat))
                .Concat(selection.BlockedIds.Select(id => BlockedPrefix + id.ToString(GuidFormat))));
}
