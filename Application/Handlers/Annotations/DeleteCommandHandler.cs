// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes one client note. A note owned by a client outside the caller's group visibility is refused
/// exactly like a note that does not exist, and nothing is deleted.
/// </summary>
/// <param name="request">Carries the id of the note to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Annotations;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IAnnotationRepository annotationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnnotationResource?> Handle(DeleteCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAnnotation = await _annotationRepository.Get(request.Id);
            if (existingAnnotation == null
                || !await _clientVisibilityGuard.IsVisibleAsync(existingAnnotation.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Annotation with ID {request.Id} not found.");
            }

            var annotationResource = _settingsMapper.ToAnnotationResource(existingAnnotation);
            await _annotationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return annotationResource;
        },
        "deleting annotation",
        new { AnnotationId = request.Id });
    }
}
