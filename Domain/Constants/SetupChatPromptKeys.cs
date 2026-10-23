// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Frontend i18n keys of the chat requests Klacksy sends on the user's behalf when a goal or a toast hands over to the
/// chat. The frontend translates the key into the user's language and submits it as a chat turn. OrdersReviewRequest
/// asks Klacksy to go through the open orders with the user before anything is sealed; it never asks to seal. Klacksy
/// cannot mass-seal at all since the owner decision of 2026-10-23 (seal_open_orders is switched off in its seed); the
/// guided review that may seal is a separate, later step. A single order is sealed only by seal_shift, as the confirmed
/// replay of its server preview after the user's explicit yes in a later turn.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SetupChatPromptKeys
{
    public const string OrdersReviewRequest = "klacksy.orders.review.request";

    public const string SetupConsultationTrigger = "setupConsultation.triggerPhrase";
}
