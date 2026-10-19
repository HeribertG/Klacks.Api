// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Imports;

public record UploadErpOrderFileToDefaultCommand(string FileName, Stream Content, string? UploadedBy) : IRequest<string>;
