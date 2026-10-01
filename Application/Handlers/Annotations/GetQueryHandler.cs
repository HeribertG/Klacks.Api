// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one client note by its id. A note owned by a client outside the caller's group visibility is
/// answered exactly like a note that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<AnnotationResource>, AnnotationResource>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly SettingsMapper _settingsMapper;

        public GetQueryHandler(
            IAnnotationRepository annotationRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            SettingsMapper settingsMapper,
            ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _annotationRepository = annotationRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _settingsMapper = settingsMapper;
        }

        public async Task<AnnotationResource> Handle(GetQuery<AnnotationResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var annotation = await _annotationRepository.Get(request.Id);

                if (annotation == null
                    || !await _clientVisibilityGuard.IsVisibleAsync(annotation.ClientId, cancellationToken))
                {
                    throw new KeyNotFoundException($"Annotation with ID {request.Id} not found");
                }

                return _settingsMapper.ToAnnotationResource(annotation);
            }, nameof(Handle), new { request.Id });
        }
    }
}
