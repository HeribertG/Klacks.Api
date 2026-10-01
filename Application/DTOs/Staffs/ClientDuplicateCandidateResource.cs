// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

/// <summary>
/// One hit of the duplicate check when a new client is created. Identity only: the check is deliberately not
/// limited by group visibility (owner decision 2026-10-01), so it must not carry anything beyond who the person is.
/// The internal id is only returned for a client the caller may open; a hit in a foreign group carries none.
/// </summary>
public sealed record ClientDuplicateCandidateResource(
    Guid? Id,
    int IdNumber,
    string? Company,
    string? Name,
    string? FirstName,
    int Type);
