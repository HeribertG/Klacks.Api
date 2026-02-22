using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeactivateSoulSectionCommandHandler : IRequestHandler<DeactivateSoulSectionCommand, Unit>
{
    private readonly IAgentSoulRepository _soulRepository;

    public DeactivateSoulSectionCommandHandler(IAgentSoulRepository soulRepository)
    {
        _soulRepository = soulRepository;
    }

    public async Task<Unit> Handle(DeactivateSoulSectionCommand request, CancellationToken cancellationToken)
    {
        await _soulRepository.DeactivateSectionAsync(request.AgentId, request.SectionType, cancellationToken);
        return Unit.Value;
    }
}
