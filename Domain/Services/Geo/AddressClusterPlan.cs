// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Geo;

public sealed record AddressClusterPlan(
    IReadOnlyList<AddressCluster> Clusters,
    IReadOnlyList<AddressClusterAssignment> Assignments,
    IReadOnlyList<AddressClusterRejection> Rejections);
