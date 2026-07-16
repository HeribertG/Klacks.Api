// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a calculation macro; validates the macro script (compile + probe execution)
/// before persisting.
/// </summary>
/// <param name="request">Contains the macro resource with id, name, content (script) and type</param>

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
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly IMacroScriptValidator _macroScriptValidator;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
            ISettingsRepository settingsRepository,
            SettingsMapper settingsMapper,
            IMacroScriptValidator macroScriptValidator,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _macroScriptValidator = macroScriptValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            EnsureValidScript(request.model.Content);

            var macro = _settingsMapper.ToMacroEntity(request.model);
            var updatedMacro = await _settingsRepository.PutMacroAsync(macro);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToMacroResource(updatedMacro);
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
