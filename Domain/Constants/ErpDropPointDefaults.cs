// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
