// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAnswerGroundingSentinelProbe
{
    Task<bool> RunAsync(CancellationToken cancellationToken = default);
}
