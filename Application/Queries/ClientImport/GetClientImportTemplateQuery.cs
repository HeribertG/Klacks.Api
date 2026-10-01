// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Requests the xlsx import template.
/// </summary>
/// <param name="Language">Explicitly requested template language; null or blank when the caller did not choose one</param>
/// <param name="PreferredLanguages">Languages of the request's Accept-Language header, best first; used when no language was requested</param>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientImport;

public record GetClientImportTemplateQuery(string? Language, IReadOnlyList<string>? PreferredLanguages = null) : IRequest<ClientImportTemplateFile>;
