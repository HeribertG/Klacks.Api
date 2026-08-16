// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One qualification bucket in a qualification-group-candidate evaluation: how many clients currently
/// hold this qualification (valid today), and whether that count meets the minimum viable group size.
/// </summary>
/// <param name="QualificationId">Id of the qualification, for direct use as fill_group_by_criteria's qualificationId</param>
/// <param name="QualificationName">Display name of the qualification (resolved via QualificationResolver.DisplayName)</param>
/// <param name="ClientCount">Number of clients currently holding this qualification (valid today)</param>
/// <param name="IsViable">True when ClientCount meets GroupingAdvisoryDefaults.MinViableGroupSize</param>
public sealed record QualificationGroupCandidate(
    Guid QualificationId,
    string QualificationName,
    int ClientCount,
    bool IsViable);
