// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes the notes of one client and nothing else. The note card of edit-address saves through the
/// client aggregate, so a caller who may write notes but not clients sends a whole ClientResource; this
/// command is the narrow path that request takes, carrying only what such a caller is allowed to
/// change. Every other field of the stored client is left untouched by construction rather than by a
/// comparison that could be wrong.
/// </summary>
/// <param name="ClientId">Client whose notes are written</param>
/// <param name="Annotations">Complete note list as it should be afterwards; a stored note missing from it is soft-deleted</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Clients;

public record UpdateClientAnnotationsCommand(Guid ClientId, IReadOnlyList<AnnotationResource> Annotations)
    : IRequest<ClientResource?>;
