// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// How many open orders a single group would receive, or did receive.
/// </summary>
/// <param name="GroupName">Name of the target group.</param>
/// <param name="GroupId">Id of the target group.</param>
/// <param name="OrderCount">Number of orders placed into that group.</param>
public sealed record OrderGroupTargetSummary(string GroupName, Guid GroupId, int OrderCount);
