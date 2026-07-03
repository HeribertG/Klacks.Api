// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Supplies the single mailbox drop point for ERP order imports: returns the oldest existing
/// drop point or creates one with the shared default values when none exists yet.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpDefaultDropPointProvider : IErpDefaultDropPointProvider
{
    private readonly IErpDropPointRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ErpDefaultDropPointProvider(IErpDropPointRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErpDropPoint> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default)
    {
        var existing = (await _repository.List())
            .OrderBy(dropPoint => dropPoint.CreateTime)
            .FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        var dropPoint = new ErpDropPoint
        {
            Id = Guid.NewGuid(),
            Name = ErpDropPointDefaults.Name,
            SourceSystemId = ErpDropPointDefaults.SourceSystemId,
            BucketPrefix = ErpDropPointDefaults.BucketPrefix,
            IsEnabled = ErpDropPointDefaults.IsEnabled
        };

        await _repository.Add(dropPoint);
        await _unitOfWork.CompleteAsync();

        return dropPoint;
    }
}
