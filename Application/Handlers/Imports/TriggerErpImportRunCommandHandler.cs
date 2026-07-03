// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that moves the ERP import schedule's next-run marker to the current UTC time so the
/// next background tick fires the import immediately. Writes the setting exactly like the
/// runner's own SaveNextRunAsync: round-trip ("O") format, update when the row exists,
/// insert otherwise, then persist via the unit of work.
/// </summary>
/// <param name="request">Marker command without parameters</param>

using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class TriggerErpImportRunCommandHandler : IRequestHandler<TriggerErpImportRunCommand, Unit>
{
    private const string RoundtripFormat = "O";

    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TriggerErpImportRunCommandHandler(ISettingsRepository settingsRepository, IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(TriggerErpImportRunCommand request, CancellationToken cancellationToken)
    {
        var value = DateTime.UtcNow.ToString(RoundtripFormat);
        var existing = await _settingsRepository.GetSetting(ErpImportSettingsTypes.NextRunUtc);

        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            await _settingsRepository.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = ErpImportSettingsTypes.NextRunUtc,
                Value = value
            });
        }

        await _unitOfWork.CompleteAsync();
        return Unit.Value;
    }
}
