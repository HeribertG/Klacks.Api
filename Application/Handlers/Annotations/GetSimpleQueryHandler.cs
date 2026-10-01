// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the notes of one client. A client outside the caller's group visibility is answered like a
/// client without notes: the list is empty and the notes are never read.
/// </summary>
/// <param name="request">Carries the id of the client whose notes are listed</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetSimpleQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetSimpleQueryHandler> _logger;

        public GetSimpleQueryHandler(
            IAnnotationRepository annotationRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            SettingsMapper settingsMapper,
            ILogger<GetSimpleQueryHandler> logger)
        {
            _annotationRepository = annotationRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching simple annotations for ID: {Id}", request.Id);

                if (request.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid ID provided for simple annotation list: empty GUID");
                    throw new InvalidRequestException("ID cannot be empty for simple annotation list query");
                }

                if (!await _clientVisibilityGuard.IsVisibleAsync(request.Id, cancellationToken))
                {
                    return [];
                }

                var annotations = await _annotationRepository.SimpleList(request.Id);

                _logger.LogInformation("Retrieved {Count} simple annotations for ID: {Id}", annotations.Count, request.Id);
                return _settingsMapper.ToAnnotationResources(annotations.ToList());
            }
            catch (InvalidRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching simple annotations for ID: {Id}", request.Id);
                throw new InvalidRequestException($"Failed to retrieve simple annotations for ID {request.Id}: {ex.Message}");
            }
        }
    }
}
