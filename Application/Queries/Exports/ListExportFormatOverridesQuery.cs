// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record ListExportFormatOverridesQuery : IRequest<ExportFormatOverrideCatalog>;
