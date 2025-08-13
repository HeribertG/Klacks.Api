using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks
{
    public class GetQueryHandler : IRequestHandler<GetQuery<BreakResource>, BreakResource>
    {
        private readonly BreakApplicationService _breakApplicationService;

        public GetQueryHandler(BreakApplicationService breakApplicationService)
        {
            _breakApplicationService = breakApplicationService;
        }

        public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
        {
            return await _breakApplicationService.GetBreakByIdAsync(request.Id, cancellationToken) ?? new BreakResource();
        }
    }
}
