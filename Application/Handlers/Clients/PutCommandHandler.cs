using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Clients;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<ClientResource?> Handle(PutCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var client = _mapper.Map<Klacks.Api.Domain.Models.Staffs.Client>(request.Resource);
            var updatedClient = await _clientRepository.Put(client);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ClientResource>(updatedClient);
        }, 
        "updating", 
        new { });
    }
}
