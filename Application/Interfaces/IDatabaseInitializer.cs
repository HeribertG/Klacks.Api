// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Creates and migrates the database and runs the one-time system seed (DatabaseInitializer in Infrastructure).
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync();

    Task SeedDataAsync();
}
