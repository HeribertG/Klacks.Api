// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Persistence;

public static class DataProtectionKeySchema
{
    public const string TableName = "data_protection_keys";
    public const string IdColumn = "id";
    public const string FriendlyNameColumn = "friendly_name";
    public const string XmlColumn = "xml";
}
