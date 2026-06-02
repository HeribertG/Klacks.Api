// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

/// <summary>
/// Creates a qualification master entry, or returns the existing one if a qualification with the
/// same German name (case-insensitive) already exists, so repeated calls do not create duplicates.
/// Returns the qualification id.
/// </summary>
/// <param name="Name">Multilingual qualification name (e.g. "Forklift licence")</param>
/// <param name="Description">Optional multilingual description</param>
public record CreateQualificationCommand(
    MultiLanguage Name,
    MultiLanguage? Description) : IRequest<Guid>;
