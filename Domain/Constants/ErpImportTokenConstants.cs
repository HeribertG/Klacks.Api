// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for ERP import tokens: a deliberately separate token universe from Personal
/// Access Tokens (different prefix, different scheme, different table) so a token handed to
/// an external ERP vendor can authenticate the upload endpoint and nothing else.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportTokenConstants
{
    public const string TokenPrefix = "klacks_erp_";

    public const string SchemeName = "KlacksErpImport";

    public const string DropPointIdClaimType = "erp_drop_point_id";

    public const int TokenByteLength = 32;

    public const int DisplayPrefixLength = 16;

    public const int DefaultExpiryDays = 365;

    public const int MinExpiryDays = 1;

    public const int MaxExpiryDays = 730;
}
