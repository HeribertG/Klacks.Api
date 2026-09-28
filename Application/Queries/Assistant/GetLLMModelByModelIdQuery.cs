// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public record GetLLMModelByModelIdQuery(string ModelId) : IRequest<LLMModel?>;