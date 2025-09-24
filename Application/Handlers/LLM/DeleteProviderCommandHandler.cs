using MediatR;
using Klacks.Api.Domain.Interfaces;

using Klacks.Api.Application.Commands.LLM;
namespace Klacks.Api.Application.Handlers.LLM;

public class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand, bool>
{
    private readonly ILLMRepository _repository;

    public DeleteProviderCommandHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
    {
        return await _repository.DeleteProviderAsync(request.Id);
    }
}