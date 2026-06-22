// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class GetQueryHandler : IRequestHandler<GetQuery, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ISettingsRepository settingsRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<MacroResource?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching macro with ID: {Id}", request.Id);
            
            var macro = await _settingsRepository.GetMacro(request.Id);

            if (macro == null)
            {
                _logger.LogWarning("Macro with ID {Id} not found", request.Id);
                return null;
            }

            _logger.LogInformation("Successfully retrieved macro with ID: {Id}", request.Id);
            return _settingsMapper.ToMacroResource(macro);
        }
    }
}
