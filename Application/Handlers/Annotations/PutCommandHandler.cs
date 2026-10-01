// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates one client note. The resource carries the owning client id, so an update could move a note
/// onto or away from another client: both the stored owner and the incoming owner must be inside the
/// caller's group visibility, otherwise the request is refused exactly like a missing note and nothing
/// is written.
/// </summary>
/// <param name="request">Carries the note with its new values, including the owning client id</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IAnnotationRepository annotationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnnotationResource?> Handle(PutCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAnnotation = await _annotationRepository.Get(request.Resource.Id);
            if (existingAnnotation == null
                || !await _clientVisibilityGuard.AreAllVisibleAsync(
                    [existingAnnotation.ClientId, request.Resource.ClientId], cancellationToken))
            {
                throw new KeyNotFoundException($"Annotation with ID {request.Resource.Id} not found.");
            }

            _settingsMapper.UpdateAnnotationEntity(request.Resource, existingAnnotation);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToAnnotationResource(existingAnnotation);
        },
        "updating annotation",
        new { AnnotationId = request.Resource.Id });
    }
}
