// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Data.Seed
{
    public static class FakeSettings
    {
        public static string ClientsNumber { get; set; } = string.Empty;

        public static string MaxBreaksPerClientPerYear { get; set; } = "30";

        public static bool UseDumpFile { get; set; } = true;
    }
}
