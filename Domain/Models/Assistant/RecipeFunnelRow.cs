// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record RecipeFunnelRow(
    string RecipeName,
    int Started,
    int Running,
    int Completed,
    int Aborted,
    int Expired);
