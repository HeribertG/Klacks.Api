using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CommunicationResource>, IEnumerable<CommunicationResource>>
    {
        private readonly CommunicationApplicationService _communicationApplicationService;

        public GetListQueryHandler(CommunicationApplicationService communicationApplicationService)
        {
            _communicationApplicationService = communicationApplicationService;
        }

        public async Task<IEnumerable<CommunicationResource>> Handle(ListQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            return await _communicationApplicationService.GetAllCommunicationsAsync(cancellationToken);
        }
    }
}
