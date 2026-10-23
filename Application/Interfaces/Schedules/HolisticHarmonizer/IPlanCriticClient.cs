// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

namespace Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;

/// <summary>
/// Sends the anonymized plan to a vision-capable LLM and returns its findings unverified. Implementations never throw
/// for provider or parse problems; they report them in <see cref="PlanCriticClientResponse.Error"/>.
/// </summary>
public interface IPlanCriticClient
{
    Task<PlanCriticClientResponse> ReviewAsync(PlanCriticRequest request, CancellationToken cancellationToken);
}
