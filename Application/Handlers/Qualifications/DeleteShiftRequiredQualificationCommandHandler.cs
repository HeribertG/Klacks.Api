// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="DeleteShiftRequiredQualificationCommand"/>. Soft-deletes the row; the
/// soft-delete timestamp and user are applied automatically by the DbContext save pipeline. A row of a shift a
/// group-restricted caller may not see is left untouched, exactly like a missing row (no error either way); so is a row of
/// a shift with an analyse token (of a variant or a scenario clone), for every caller: its requirements go through the shift.
/// </summary>
/// <param name="repository">Provides access to the shift-required-qualification store</param>
/// <param name="unitOfWork">Commits the change</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller may touch the row's shift and whether it carries a token</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class DeleteShiftRequiredQualificationCommandHandler
    : IRequestHandler<DeleteShiftRequiredQualificationCommand>
{
    private readonly IShiftRequiredQualificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public DeleteShiftRequiredQualificationCommandHandler(
        IShiftRequiredQualificationRepository repository,
        IUnitOfWork unitOfWork,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<Unit> Handle(
        DeleteShiftRequiredQualificationCommand request, CancellationToken cancellationToken)
    {
        var stored = await _repository.Get(request.Id);
        if (stored != null
            && (!await _shiftVisibilityGuard.IsVisibleAsync(stored.ShiftId, cancellationToken)
                || await _shiftVisibilityGuard.FindFirstTokenedAsync([stored.ShiftId], cancellationToken) is not null))
        {
            return Unit.Value;
        }

        await _repository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return Unit.Value;
    }
}
