using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetQueryHandler : IRequestHandler<GetQuery<SelectedCalendarResource>, SelectedCalendarResource>
    {
        private readonly ISelectedCalendarRepository _selectedCalendarRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ISelectedCalendarRepository selectedCalendarRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
        {
            _selectedCalendarRepository = selectedCalendarRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<SelectedCalendarResource> Handle(GetQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting selected calendar with ID: {Id}", request.Id);
                
                var selectedCalendar = await _selectedCalendarRepository.Get(request.Id);
                
                if (selectedCalendar == null)
                {
                    throw new KeyNotFoundException($"Selected calendar with ID {request.Id} not found");
                }
                
                var result = _mapper.Map<SelectedCalendarResource>(selectedCalendar);
                _logger.LogInformation("Successfully retrieved selected calendar with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving selected calendar with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving selected calendar with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
