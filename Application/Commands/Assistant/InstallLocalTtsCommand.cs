// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record InstallLocalTtsCommand(bool AcceptLicense, string? LicenseVersion, string RequestedBy) : IRequest<LocalTtsInstallResult>;
