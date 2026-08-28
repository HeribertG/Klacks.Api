// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one maintenance run of the learning loop did.
/// </summary>
/// <param name="Promoted">Clusters moved from collecting to ready because they reached the threshold</param>
/// <param name="Retired">Terminal clusters soft-deleted because they passed the retention window</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningMaintenanceResult(int Promoted, int Retired);
