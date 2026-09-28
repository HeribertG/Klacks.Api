// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Inbound;

public record InboundActionOutcome(bool Executed, string Description);
