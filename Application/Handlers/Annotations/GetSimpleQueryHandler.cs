using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetSimpleQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetSimpleQueryHandler> _logger;

        public GetSimpleQueryHandler(IAnnotationRepository annotationRepository, IMapper mapper, ILogger<GetSimpleQueryHandler> logger)
        {
            _annotationRepository = annotationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Fetching simple annotations for ID: {request.Id}");
                
                if (request.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid ID provided for simple annotation list: empty GUID");
                    throw new InvalidRequestException("ID cannot be empty for simple annotation list query");
                }
                
                var annotations = await _annotationRepository.SimpleList(request.Id);
                
                _logger.LogInformation($"Retrieved {annotations.Count} simple annotations for ID: {request.Id}");
                return _mapper.Map<IEnumerable<AnnotationResource>>(annotations);
            }
            catch (InvalidRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching simple annotations for ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve simple annotations for ID {request.Id}: {ex.Message}");
            }
        }
    }
}
