using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

public class PostCommandHandler : IRequestHandler<PostCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _mapper.Map<Klacks.Api.Domain.Models.Staffs.Client>(request.Resource);
            await _clientRepository.Add(client);
            await _unitOfWork.CompleteAsync();
            var createdClient = _mapper.Map<ClientResource>(client);
            _logger.LogInformation("New client created successfully. ID: {ClientId}", createdClient.Id);
            return createdClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a new client.");
            throw;
        }
    }
}
