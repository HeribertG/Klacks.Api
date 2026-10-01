// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ClientImport;

public record CommitClientImportCommand(ClientImportRequest Request) : IRequest<ClientImportCommitResult>;
