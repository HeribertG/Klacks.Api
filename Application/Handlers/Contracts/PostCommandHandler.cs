using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PostCommandHandler : IRequestHandler<PostCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IContractRepository contractRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _contractRepository = contractRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractResource?> Handle(PostCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = _mapper.Map<Contract>(request.Resource);
            await _contractRepository.Add(contract);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ContractResource>(contract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new contract.");
            throw;
        }
    }
}