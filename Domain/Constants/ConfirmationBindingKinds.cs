// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The kinds of facts a confirmation can be bound to (ConfirmationBinding.Kind). A skill accepts only the kind its own
/// preview provider produces, so a binding made for one skill can never pin another. Message is the kind the messaging plugin
/// binds send_message with; a test pins it equal to the plugin's own constant. OpenOrdersSeal binds seal_open_orders to the open
/// orders its preview classified (sealable or blocked), OrderSeal binds seal_shift to the one order its preview showed.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ConfirmationBindingKinds
{
    public const string Work = "work";

    public const string MacroSwitch = "macro-switch";

    public const string MacroUndo = "macro-undo";

    public const string Message = "message";

    public const string OpenOrdersSeal = "open-orders-seal";

    public const string OrderSeal = "order-seal";
}
