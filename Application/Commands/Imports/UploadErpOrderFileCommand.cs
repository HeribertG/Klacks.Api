// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Imports;

public record UploadErpOrderFileCommand(
    Guid DropPointId,
    string FileName,
    Stream Content,
    ErpImportChannel Channel,
    string? UploadedBy) : IRequest<string>;
