using MediatR;

namespace Klacks.Api.Application.Commands.LLM;

public class DeleteProviderCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public DeleteProviderCommand(Guid id)
    {
        Id = id;
    }
}