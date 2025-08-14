using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class GetQueryHandler : IRequestHandler<GetQuery<WorkResource>, WorkResource>
{
    private readonly IWorkRepository _workRepository;
    private readonly IMapper _mapper;

    public GetQueryHandler(IWorkRepository workRepository, IMapper mapper)
    {
        _workRepository = workRepository;
        _mapper = mapper;
    }

    public async Task<WorkResource> Handle(GetQuery<WorkResource> request, CancellationToken cancellationToken)
    {
        var work = await _workRepository.Get(request.Id);
        
        if (work == null)
        {
            return new WorkResource();
        }
        
        return _mapper.Map<WorkResource>(work);
    }
}
