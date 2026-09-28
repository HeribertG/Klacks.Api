// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
