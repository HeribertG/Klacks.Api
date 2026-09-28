// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public record GetLLMModelQuery(Guid Id) : GetQuery<LLMModel>(Id);