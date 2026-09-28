// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Imports;

public record UploadErpOrderFileCommand(Guid DropPointId, string FileName, Stream Content) : IRequest<string>;
