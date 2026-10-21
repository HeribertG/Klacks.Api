// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.Controllers.UserBackend.Imports;

public record CreateErpImportTokenRequest(Guid DropPointId, string Name, int? ExpiresInDays);
