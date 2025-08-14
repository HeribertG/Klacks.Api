using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IMapper _mapper;

    public PostCommandHandler(IWorkRepository workRepository, IMapper mapper)
    {
        _workRepository = workRepository;
        _mapper = mapper;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        var work = _mapper.Map<Klacks.Api.Domain.Models.Schedules.Work>(request.Resource);
        await _workRepository.Add(work);
        return _mapper.Map<WorkResource>(work);
    }
}
