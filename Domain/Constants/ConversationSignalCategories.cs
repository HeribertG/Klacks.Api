// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names of the negating conversation-signal lists a language pack ships, as they appear in conversation-signals.json
/// and in the NegatingCategories of the generated collision table.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ConversationSignalCategories
{
    public const string Negations = "negations";

    public const string Declines = "declines";

    public const string Cancellations = "cancellations";

    public const string Corrections = "corrections";
}
