// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Interface for skill execution logic only. Metadata (name, description, parameters)
/// comes from the database. Implementations are matched to DB entries via [SkillImplementation] attribute.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillImplementation
{
    Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
