// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure decision logic determining whether demo/training data (fake clients, shifts, contracts) is
/// seeded, and whether that demo data includes groups/group items/shifts on top of the clients: a
/// configured region setup profile is authoritative, otherwise the legacy fake configuration flag
/// applies. The legacy flag has no split between clients and shifts/groups, so it seeds both together.
/// </summary>
/// <param name="regionFileConfigured">True when a region setup profile file path is configured</param>
/// <param name="profileSeedDemoData">The seedDemoData value of the region setup profile, null when omitted</param>
/// <param name="profileSeedDemoShiftsAndGroups">The seedDemoShiftsAndGroups value of the region setup profile, null when omitted (then follows profileSeedDemoData)</param>
/// <param name="legacyFakeConfigEnabled">The legacy Fake:WithFake configuration flag</param>

namespace Klacks.Api.Data.Seed;

public static class DemoDataSeedDecision
{
    public static (bool SeedDemoClients, bool SeedDemoShiftsAndGroups, DemoDataSeedSource Source) Decide(
        bool regionFileConfigured,
        bool? profileSeedDemoData,
        bool? profileSeedDemoShiftsAndGroups,
        bool legacyFakeConfigEnabled)
    {
        if (regionFileConfigured)
        {
            var seedDemoClients = profileSeedDemoData == true;
            var seedDemoShiftsAndGroups = seedDemoClients && (profileSeedDemoShiftsAndGroups ?? true);

            return (seedDemoClients, seedDemoShiftsAndGroups, DemoDataSeedSource.RegionSetupProfile);
        }

        return (legacyFakeConfigEnabled, legacyFakeConfigEnabled, DemoDataSeedSource.LegacyFakeConfiguration);
    }

    /// <summary>
    /// True when the demo shift plan has to be exported as an ERP order file: demo clients exist but
    /// the demo shifts that would reference them were skipped, so the plan can only arrive by import.
    /// </summary>
    /// <param name="seedDemoClients">Whether demo clients were seeded</param>
    /// <param name="seedDemoShiftsAndGroups">Whether demo groups and shifts were seeded alongside them</param>
    public static bool ShouldExportDemoOrders(bool seedDemoClients, bool seedDemoShiftsAndGroups)
    {
        return seedDemoClients && !seedDemoShiftsAndGroups;
    }
}
