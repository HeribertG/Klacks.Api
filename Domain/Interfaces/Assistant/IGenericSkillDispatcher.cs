// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGenericSkillDispatcher
{
    bool CanHandle(string? handlerType);

    Task<SkillResult> ExecuteAsync(
        string handlerType,
        string handlerConfig,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
