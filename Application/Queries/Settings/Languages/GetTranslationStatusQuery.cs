// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.Languages;

public record GetTranslationStatusQuery() : IRequest<bool>;
