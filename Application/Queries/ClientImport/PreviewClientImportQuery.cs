// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientImport;

public record PreviewClientImportQuery(ClientImportRequest Request) : IRequest<ClientImportPreviewResult>;
