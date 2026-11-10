// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads whether the own-admin setup is still required, with the password rules of the setup form.
/// </summary>
/// <param name="IsFirstStart">True for the anonymous first-start probe: it answers whether the installation can still
/// be claimed without signing in and carries the password rules only while it can</param>
/// <param name="IsLocalRequest">The probe comes straight from the machine itself, so no setup code is needed</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

public record GetSetupStatusQuery(bool IsFirstStart = false, bool IsLocalRequest = false) : IRequest<SetupStatusResource>;
