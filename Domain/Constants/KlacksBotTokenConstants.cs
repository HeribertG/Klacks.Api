// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the Klacks bot token: a deliberately separate token universe from Personal
/// Access Tokens and ERP import tokens (own prefix, own scheme, own table, no user roles)
/// so a token handed to an external bot can authenticate only the bot read-only endpoints
/// and nothing else in the API.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class KlacksBotTokenConstants
{
    public const string TokenPrefix = "klacks_bot_";

    public const string SchemeName = "KlacksBotToken";

    public const string BotIdentityClaimType = "bot_identity";

    public const int TokenByteLength = 32;

    public const int DisplayPrefixLength = 16;

    public const int DefaultExpiryDays = 90;

    public const int MinExpiryDays = 1;

    public const int MaxExpiryDays = 180;
}
