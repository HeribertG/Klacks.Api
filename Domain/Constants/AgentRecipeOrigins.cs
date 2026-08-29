// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Where a row in agent_recipes came from. The seed loader owns Seed rows and may overwrite them on a
/// version bump; everything else it must leave alone, which is what stops a learned recipe from being
/// silently reverted by the next deployment.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class AgentRecipeOrigins
{
    public const string Seed = "Seed";
    public const string Learned = "Learned";
    public const string Admin = "Admin";
}
