// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Assistant;

public sealed record PlannableClientInfo(Guid ClientId, string? FirstName, string Name);
