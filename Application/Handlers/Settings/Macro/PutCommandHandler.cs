// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
        ISettingsRepository settingsRepository,
                                  SettingsMapper settingsMapper,
                                  IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var macro = _settingsMapper.ToMacroEntity(request.model);
            var updatedMacro = _settingsRepository.PutMacro(macro);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToMacroResource(updatedMacro);
        }
    }
}
