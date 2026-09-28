// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Email;

public record TranslateReceivedEmailQuery(Guid Id, string TargetLanguage) : IRequest<TranslatedEmailResource?>;
