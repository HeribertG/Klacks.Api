using AutoMapper;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCutsCommandHandler : IRequestHandler<PostCutsCommand, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public PostCutsCommandHandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<List<ShiftResource>> Handle(PostCutsCommand request, CancellationToken cancellationToken)
    {
        var createdShifts = new List<ShiftResource>();
        
        foreach (var cutResource in request.Cuts)
        {
            var shift = _mapper.Map<Shift>(cutResource);
            await _shiftRepository.Add(shift);
            createdShifts.Add(_mapper.Map<ShiftResource>(shift));
        }
        
        return createdShifts;
    }
}