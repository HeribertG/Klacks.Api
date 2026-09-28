// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default values applied when the single mailbox drop point for ERP order imports is created
/// on demand because no drop point exists yet.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpDropPointDefaults
{
    public const string Name = "Default";

    public const string SourceSystemId = "default";

    public const string BucketPrefix = "erp/orders";

    public const bool IsEnabled = true;
}
