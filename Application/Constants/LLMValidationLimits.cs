// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Input limits enforced by the FluentValidation validators of the assistant (LLM model and provider
/// administration, usage and conversation-history queries). Shared so the create and update validators of
/// one resource cannot drift apart.
/// </summary>
public static class LLMValidationLimits
{
    public const int MaxModelIdLength = 50;

    public const int MaxModelNameLength = 100;

    public const int MaxModelCategoryLength = 50;

    public const int MaxModelDescriptionLength = 500;

    public const int MaxContextWindowTokens = 2_000_000;

    public const int TokenCostPrecision = 10;

    public const int TokenCostScale = 6;

    public const int MaxProviderIdLength = 50;

    public const int MaxProviderNameLength = 100;

    public const int MinApiKeyLength = 10;

    public const int MaxProviderPriority = 100;

    public const int MaxUsageDays = 365;

    public const int MaxConversationHistoryLimit = 100;

    /// <summary>Length of the ASP.NET Identity user id column (nvarchar(450) in the Identity schema).</summary>
    public const int MaxUserIdLength = 450;
}
