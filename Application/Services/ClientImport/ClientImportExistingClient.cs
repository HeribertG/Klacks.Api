// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public record ClientImportExistingClient(
    Guid Id,
    string? FirstName,
    string Name,
    DateTime? Birthdate,
    IReadOnlyList<string> Emails,
    EntityTypeEnum Type,
    bool LegalEntity,
    string? Company,
    IReadOnlyList<ClientImportExistingAddress> Addresses);
