// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a calculation macro; validates the macro script (compile + probe execution)
/// before persisting.
/// </summary>
/// <param name="request">Contains the macro resource with name, content (script) and type</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly IMacroScriptValidator _macroScriptValidator;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
            ISettingsRepository settingsRepository,
            SettingsMapper settingsMapper,
            IMacroScriptValidator macroScriptValidator,
            IUnitOfWork unitOfWork,
            ILogger<PostCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _macroScriptValidator = macroScriptValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            EnsureValidScript(request.model.Content);

            var macro = _settingsMapper.ToMacroEntity(request.model);
            var result = await _settingsRepository.AddMacroAsync(macro);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToMacroResource(result);
        }

        private void EnsureValidScript(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var validation = _macroScriptValidator.Validate(content);
            if (!validation.IsValid)
            {
                throw new InvalidRequestException(validation.ErrorMessage!);
            }
        }
    }
}
