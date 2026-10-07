// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ReplacementRequestFilter(
    Guid? AbsentClientId,
    DateOnly? FromDate,
    DateOnly? UntilDate,
    Guid? AnalyseToken);
