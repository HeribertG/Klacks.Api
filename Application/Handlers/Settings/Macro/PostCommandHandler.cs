// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
        ISettingsRepository settingsRepository,
                                  SettingsMapper settingsMapper,
                                  IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var macro = _settingsMapper.ToMacroEntity(request.model);
            var result = _settingsRepository.AddMacro(macro);

            await _unitOfWork.CompleteAsync();

            return _settingsMapper.ToMacroResource(result);
        }
    }
}
