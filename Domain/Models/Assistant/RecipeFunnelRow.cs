// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record RecipeFunnelRow(
    string RecipeName,
    int Started,
    int Running,
    int Completed,
    int Aborted,
    int Expired);
