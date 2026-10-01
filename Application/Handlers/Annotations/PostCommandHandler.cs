// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates one client note. A note for a client outside the caller's group visibility is refused exactly
/// like a note for a client that does not exist, and nothing is written.
/// </summary>
/// <param name="request">Carries the note, including the id of the client it belongs to</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IAnnotationRepository annotationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnnotationResource?> Handle(PostCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.Resource.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {request.Resource.ClientId} not found");
            }

            var annotation = _settingsMapper.ToAnnotationEntity(request.Resource);
            await _annotationRepository.Add(annotation);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToAnnotationResource(annotation);
        },
        "creating annotation",
        new { AnnotationId = request.Resource?.Id });
    }
}
