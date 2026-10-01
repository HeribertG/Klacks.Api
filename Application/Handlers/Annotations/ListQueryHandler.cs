// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving all annotations via the generic ListQuery. Only notes of clients inside the
/// caller's group visibility are returned.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the notes down to those whose client the caller may see</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Annotations;

public class ListQueryHandler : IRequestHandler<ListQuery<AnnotationResource>, IEnumerable<AnnotationResource>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly SettingsMapper _settingsMapper;

    public ListQueryHandler(
        IAnnotationRepository annotationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        SettingsMapper settingsMapper)
    {
        _annotationRepository = annotationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _settingsMapper = settingsMapper;
    }

    public async Task<IEnumerable<AnnotationResource>> Handle(ListQuery<AnnotationResource> request, CancellationToken cancellationToken)
    {
        var annotations = await _annotationRepository.List();
        var visibleAnnotations = await _clientVisibilityGuard.FilterVisibleAsync(
            annotations.ToList(), annotation => annotation.ClientId, cancellationToken);
        return _settingsMapper.ToAnnotationResources(visibleAnnotations);
    }
}
