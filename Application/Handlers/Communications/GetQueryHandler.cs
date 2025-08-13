using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CommunicationResource>, CommunicationResource>
    {
        private readonly CommunicationApplicationService _communicationApplicationService;

        public GetQueryHandler(CommunicationApplicationService communicationApplicationService)
        {
            _communicationApplicationService = communicationApplicationService;
        }

        public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            return await _communicationApplicationService.GetCommunicationByIdAsync(request.Id, cancellationToken) ?? new CommunicationResource();
        }
    }
}
