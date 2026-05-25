// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <summary>
/// Probes every model for Klacksy readiness, enables qualifying models, disables weak ones and
/// picks the best qualifying model as default when no qualifying default is set.
/// </summary>
public record OptimizeModelsForKlacksyCommand : IRequest<KlacksyModelCheckResponse>;
