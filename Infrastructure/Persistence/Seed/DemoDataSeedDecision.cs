// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure decision logic determining whether demo/training data (fake clients, shifts, contracts)
/// is seeded: a configured region setup profile is authoritative, otherwise the legacy fake
/// configuration flag applies.
/// </summary>
/// <param name="regionFileConfigured">True when a region setup profile file path is configured</param>
/// <param name="profileSeedDemoData">The seedDemoData value of the region setup profile, null when omitted</param>
/// <param name="legacyFakeConfigEnabled">The legacy Fake:WithFake configuration flag</param>

namespace Klacks.Api.Data.Seed;

public static class DemoDataSeedDecision
{
    public static (bool SeedDemoData, DemoDataSeedSource Source) Decide(
        bool regionFileConfigured,
        bool? profileSeedDemoData,
        bool legacyFakeConfigEnabled)
    {
        if (regionFileConfigured)
        {
            return (profileSeedDemoData == true, DemoDataSeedSource.RegionSetupProfile);
        }

        return (legacyFakeConfigEnabled, DemoDataSeedSource.LegacyFakeConfiguration);
    }
}
