using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IMapper _mapper;

    public PutCommandHandler(
        IWorkRepository workRepository, IMapper mapper,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _mapper = mapper;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        var work = _mapper.Map<Domain.Models.Schedules.Work>(request.Resource);
        var updatedWork = await _workRepository.Put(work);
        return updatedWork != null ? _mapper.Map<WorkResource>(updatedWork) : null;
    }
}
