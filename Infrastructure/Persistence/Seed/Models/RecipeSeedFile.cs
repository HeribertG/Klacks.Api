// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Root model for the recipe-seeds.json file containing all operator-authored recipe definitions.
/// </summary>
/// <param name="Version">Schema version for migration compatibility</param>
/// <param name="Recipes">List of recipe seed definitions</param>
namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class RecipeSeedFile
{
    public int Version { get; set; }

    public List<RecipeSeedDefinition> Recipes { get; set; } = new();
}
