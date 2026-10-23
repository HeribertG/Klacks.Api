// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default sizes of the assistant's lookups: how many conversation messages and sessions a repository call returns,
/// how many memories a hybrid search and an embedding backfill handle, how long a session may stay inactive before it
/// is archived, how far back usage reports reach and how many history rows the soul and rule histories return.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AssistantLimitDefaults
{
    public const int ConversationMessagesLimit = 20;

    public const int OldestMessagesLimit = 40;

    public const int UserSessionsLimit = 20;

    public const int UserSessionsListLimit = 50;

    public const int StaleSessionDays = 30;

    public const int MemorySearchLimit = 10;

    public const int PendingEmbeddingsBatchSize = 100;

    public const int HistoryLimit = 50;

    public const int UsageReportDays = 30;

    public const int ConversationHistoryRequestLimit = 10;

    public const int WeatherForecastDays = 3;

    public const int ProviderPriority = 10;

    public const int MaxLessonsPerTurn = 5;
}
