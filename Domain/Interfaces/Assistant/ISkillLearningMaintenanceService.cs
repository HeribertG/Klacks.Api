// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningMaintenanceService
{
    Task<SkillLearningMaintenanceResult> RunAsync(CancellationToken cancellationToken = default);
}
