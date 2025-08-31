using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Clients;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var client = _mapper.Map<Klacks.Api.Domain.Models.Staffs.Client>(request.Resource);
            await _clientRepository.Add(client);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ClientResource>(client);
        }, 
        "creating client", 
        new { ClientId = request.Resource?.Id });
    }
}
