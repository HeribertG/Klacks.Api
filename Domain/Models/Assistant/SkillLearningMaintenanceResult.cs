// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one maintenance run of the learning loop did.
/// </summary>
/// <param name="Promoted">Clusters moved from collecting to ready because they reached the threshold</param>
/// <param name="Retired">Terminal clusters soft-deleted because they passed the retention window</param>
/// <param name="Measured">Activated artefacts whose weekly usefulness snapshot was refreshed</param>
/// <param name="Pruned">Activated artefacts withdrawn because they went unused or proved unhelpful</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningMaintenanceResult(int Promoted, int Retired, int Measured, int Pruned);
