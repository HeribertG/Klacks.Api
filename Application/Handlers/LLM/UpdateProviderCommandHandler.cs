using MediatR;
using AutoMapper;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Handlers.LLM;

public class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand, LLMProvider?>
{
    private readonly ILLMRepository _repository;
    private readonly IMapper _mapper;

    public UpdateProviderCommandHandler(ILLMRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<LLMProvider?> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _repository.GetProviderAsync(request.Id);
        if (provider == null)
        {
            return null;
        }

        _mapper.Map(request, provider);
        
        return await _repository.UpdateProviderAsync(provider);
    }
}