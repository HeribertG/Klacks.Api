// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Frontend i18n keys of the chat requests Klacksy sends on the user's behalf for a setup stage without
/// plannable shifts. The frontend translates the key into the user's language and submits it as a chat turn, so
/// an irreversible step (sealing) always runs through the chat's own preview and confirmation instead of an
/// unattended plan. The seal request mirrors the seal_open_orders synonyms of each language, which is what lets
/// skill retrieval land on that skill.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SetupChatPromptKeys
{
    public const string SealOpenOrdersRequest = "klacksy.setup.seal-open-orders.request";

    public const string SetupConsultationTrigger = "setupConsultation.triggerPhrase";
}
