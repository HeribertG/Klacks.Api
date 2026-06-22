// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving all annotations via the generic ListQuery.
/// </summary>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Annotations;

public class ListQueryHandler : IRequestHandler<ListQuery<AnnotationResource>, IEnumerable<AnnotationResource>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly SettingsMapper _settingsMapper;

    public ListQueryHandler(IAnnotationRepository annotationRepository, SettingsMapper settingsMapper)
    {
        _annotationRepository = annotationRepository;
        _settingsMapper = settingsMapper;
    }

    public async Task<IEnumerable<AnnotationResource>> Handle(ListQuery<AnnotationResource> request, CancellationToken cancellationToken)
    {
        var annotations = await _annotationRepository.List();
        return _settingsMapper.ToAnnotationResources(annotations.ToList());
    }
}
