using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class AnnotationApplicationService
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AnnotationApplicationService> _logger;

    public AnnotationApplicationService(
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        ILogger<AnnotationApplicationService> logger)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AnnotationResource?> GetAnnotationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var annotation = await _annotationRepository.Get(id);
        return annotation != null ? _mapper.Map<AnnotationResource>(annotation) : null;
    }

    public async Task<List<AnnotationResource>> GetAllAnnotationsAsync(CancellationToken cancellationToken = default)
    {
        var annotations = await _annotationRepository.List();
        return _mapper.Map<List<AnnotationResource>>(annotations);
    }

    public async Task<List<AnnotationResource>> GetSimpleAnnotationListAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var annotations = await _annotationRepository.SimpleList(clientId);
        return _mapper.Map<List<AnnotationResource>>(annotations);
    }

    public async Task<AnnotationResource> CreateAnnotationAsync(AnnotationResource annotationResource, CancellationToken cancellationToken = default)
    {
        var annotation = _mapper.Map<Annotation>(annotationResource);
        await _annotationRepository.Add(annotation);
        return _mapper.Map<AnnotationResource>(annotation);
    }

    public async Task<AnnotationResource> UpdateAnnotationAsync(AnnotationResource annotationResource, CancellationToken cancellationToken = default)
    {
        var annotation = _mapper.Map<Annotation>(annotationResource);
        var updatedAnnotation = await _annotationRepository.Put(annotation);
        return _mapper.Map<AnnotationResource>(updatedAnnotation);
    }

    public async Task DeleteAnnotationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _annotationRepository.Delete(id);
    }
}