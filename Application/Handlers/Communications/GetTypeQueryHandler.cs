using Klacks.Api.Application.Queries.Communications;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetTypeQueryHandler : IRequestHandler<GetTypeQuery, IEnumerable<CommunicationTypeResource>>
    {
        private readonly CommunicationApplicationService _communicationApplicationService;

        public GetTypeQueryHandler(CommunicationApplicationService communicationApplicationService)
        {
            _communicationApplicationService = communicationApplicationService;
        }

        public async Task<IEnumerable<CommunicationTypeResource>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
        {
            return await _communicationApplicationService.GetCommunicationTypesAsync(cancellationToken);
        }
    }
}
