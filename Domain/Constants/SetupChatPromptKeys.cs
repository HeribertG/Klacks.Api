// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Frontend i18n keys of the chat requests Klacksy sends on the user's behalf when a goal or a toast hands over to the
/// chat. The frontend translates the key into the user's language and submits it as a chat turn. OrdersReviewRequest
/// asks Klacksy to go through the open orders with the user before anything is sealed; it never asks to seal, so the
/// most it can lead to on its own is the seal_open_orders preview - sealing itself runs only as the confirmed replay of
/// the server preview, after the user's explicit yes in a later turn. Its wording names the open orders and sealing in
/// each language, which is what lets skill retrieval land on seal_open_orders for that preview.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SetupChatPromptKeys
{
    public const string OrdersReviewRequest = "klacksy.orders.review.request";

    public const string SetupConsultationTrigger = "setupConsultation.triggerPhrase";
}
