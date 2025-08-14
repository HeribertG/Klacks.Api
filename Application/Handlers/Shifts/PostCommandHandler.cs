using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCommandHandler : IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public PostCommandHandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var shift = _mapper.Map<Shift>(request.Resource);
        await _shiftRepository.Add(shift);
        return _mapper.Map<ShiftResource>(shift);
    }
}
