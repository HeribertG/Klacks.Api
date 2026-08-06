// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IKlacksSelfApiClient
{
    Task<SelfApiResult<T>> PostAsync<T>(
        string route,
        object body,
        SkillExecutionContext context,
        string skillName,
        CancellationToken cancellationToken = default);

    Task<SelfApiResult<T>> PutAsync<T>(
        string route,
        object body,
        SkillExecutionContext context,
        string skillName,
        CancellationToken cancellationToken = default);

    Task<SelfApiResult<T>> DeleteAsync<T>(
        string route,
        SkillExecutionContext context,
        string skillName,
        CancellationToken cancellationToken = default);
}
