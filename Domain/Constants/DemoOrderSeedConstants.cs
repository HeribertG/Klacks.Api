// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Identifiers and file locations for the demo order XML export that replaces the demo shift seed
/// when demo data is restricted to clients.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class DemoOrderSeedConstants
{
    public const string SourceSystemId = "KLACKS_DEMO_SEED";

    public const int DemoRandomSeed = 20250101;

    public const int FirstDayOfMonth = 1;

    public const string OrderReferencePrefix = "DEMO-";

    public const string CustomerReferencePrefix = "DEMO-CUST-";

    public const string OrderReferenceNumberFormat = "D4";

    public const string CustomerReferenceNumberFormat = "D5";

    public const string SeedDataDirectoryName = "SeedData";

    public const string DemoOrdersFileName = "demo-orders.xml";
}
