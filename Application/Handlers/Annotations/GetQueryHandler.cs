using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AnnotationResource>, AnnotationResource>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IAnnotationRepository annotationRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
        {
            _annotationRepository = annotationRepository;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<AnnotationResource> Handle(GetQuery<AnnotationResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting annotation with ID: {Id}", request.Id);
                
                var annotation = await _annotationRepository.Get(request.Id);
                
                if (annotation == null)
                {
                    throw new KeyNotFoundException($"Annotation with ID {request.Id} not found");
                }
                
                var result = _settingsMapper.ToAnnotationResource(annotation);
                _logger.LogInformation("Successfully retrieved annotation with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving annotation with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving annotation with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
