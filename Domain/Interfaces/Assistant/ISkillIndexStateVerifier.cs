// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Tells whether the knowledge index currently holds a given description for a skill. A catalogue refresh
/// returns normally even when its index sync failed, so a gate that measures right after a change has to ask.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillIndexStateVerifier
{
    Task<bool> IsIndexedAsync(string skillName, string description, CancellationToken cancellationToken = default);
}
