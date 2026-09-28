// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant.Learning;

public sealed record GetLearnedPhrasesQuery(int Limit) : IRequest<IReadOnlyList<LearnedPhraseDto>>;
