// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.Controllers.UserBackend.Bots;

public record CreateKlacksBotTokenRequest(string Name, int? ExpiresInDays);
