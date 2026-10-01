// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.ClientImport;

public record ClientImportExistingClient(Guid Id, string? FirstName, string Name, DateTime? Birthdate, IReadOnlyList<string> Emails);
