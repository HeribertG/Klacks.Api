// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        private readonly SettingsMapper _settingsMapper;

        public GetQueryHandler(IAnnotationRepository annotationRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _annotationRepository = annotationRepository;
            _settingsMapper = settingsMapper;
        }

        public async Task<AnnotationResource> Handle(GetQuery<AnnotationResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var annotation = await _annotationRepository.Get(request.Id);

                if (annotation == null)
                {
                    throw new KeyNotFoundException($"Annotation with ID {request.Id} not found");
                }

                return _settingsMapper.ToAnnotationResource(annotation);
            }, nameof(Handle), new { request.Id });
        }
    }
}
