// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes an employee import, all or nothing. The cheap request guard and the token check run before
/// the expensive evaluation, which is exactly the preview's; if a row that is not skipped still has an
/// error, nothing is written. The batch record with the token from Parse and every client aggregate are
/// saved in one transaction, so a double click fails on the unique token index (409) instead of
/// importing twice; any other unique violation is not an "already committed" and is rethrown. After the
/// commit, like a manual client create, inbox e-mails are re-assigned to clients when e-mail addresses
/// were imported, and the new addresses are offered to the background geocoding queue without waiting:
/// a full or disabled queue never delays the response, and GeocodingQueued counts only accepted ids.
/// Both follow-ups only log on failure: the import is already committed.
/// </summary>
/// <param name="evaluator">Shared evaluation path of preview and commit</param>
/// <param name="clientRepository">Stages each client aggregate (validators, IdNumber by sequence)</param>
/// <param name="batchRepository">Stages the batch record and checks the token</param>
/// <param name="unitOfWork">Owns the transaction</param>
/// <param name="emailAssignmentService">Assigns inbox e-mails to clients by address</param>
/// <param name="geocodingQueue">Background geocoding of the new addresses</param>

using Klacks.Api.Application.Commands.ClientImport;
using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientImport;

public class CommitClientImportCommandHandler : BaseHandler, IRequestHandler<CommitClientImportCommand, ClientImportCommitResult>
{
    private readonly ClientImportEvaluator _evaluator;
    private readonly IClientRepository _clientRepository;
    private readonly IClientImportBatchRepository _batchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;
    private readonly IAddressGeocodingQueue _geocodingQueue;

    public CommitClientImportCommandHandler(
        ClientImportEvaluator evaluator,
        IClientRepository clientRepository,
        IClientImportBatchRepository batchRepository,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        IAddressGeocodingQueue geocodingQueue,
        ILogger<CommitClientImportCommandHandler> logger)
        : base(logger)
    {
        _evaluator = evaluator;
        _clientRepository = clientRepository;
        _batchRepository = batchRepository;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
        _geocodingQueue = geocodingQueue;
    }

    public async Task<ClientImportCommitResult> Handle(CommitClientImportCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var importRequest = request.Request;
            ClientImportRequestGuard.EnsureValid(importRequest);

            if (await _batchRepository.ExistsByTokenAsync(importRequest.Token, cancellationToken))
            {
                throw new ClientImportConflictException($"The import {importRequest.Token} was already committed.");
            }

            var drafts = await _evaluator.EvaluateAsync(importRequest, cancellationToken);

            var errorRows = drafts.Count(d => d.Status == ClientImportRowStatus.Error);
            if (errorRows > 0)
            {
                throw new ClientImportRejectedException(ClientImportErrorCodes.RowsHaveErrors, $"{errorRows} row(s) still have errors; nothing was imported.");
            }

            var clients = drafts
                .Where(d => d.Status == ClientImportRowStatus.Ready)
                .Select(d => ClientImportClientFactory.Create(d, importRequest.Policy))
                .ToList();

            var batch = new ClientImportBatch
            {
                Id = Guid.NewGuid(),
                Token = importRequest.Token,
                FileName = ClientImportFileName.Sanitize(importRequest.FileName),
                RowCount = drafts.Count,
                CreatedCount = clients.Count,
                SkippedCount = drafts.Count - clients.Count
            };

            await SaveAsync(batch, clients, cancellationToken);

            _logger.LogInformation("Client import {BatchId} committed: {Created} created, {Skipped} skipped",
                batch.Id, batch.CreatedCount, batch.SkippedCount);

            await AssignInboxEmailsAsync(clients);
            var queued = QueueGeocoding(clients);

            return new ClientImportCommitResult
            {
                BatchId = batch.Id,
                Created = batch.CreatedCount,
                Skipped = batch.SkippedCount,
                GeocodingQueued = queued
            };
        },
        "committing client import",
        new { importToken = request.Request.Token, RowCount = request.Request.Rows?.Count });
    }

    private async Task SaveAsync(ClientImportBatch batch, List<Client> clients, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _batchRepository.AddAsync(batch, cancellationToken);
                foreach (var client in clients)
                {
                    await _clientRepository.Add(client);
                }

                await _unitOfWork.CompleteAsync();
                return true;
            });
        }
        catch (DatabaseUpdateException ex) when (ex.IsDuplicate
            && string.Equals(ex.ConstraintName, ClientImportDatabaseNames.TokenIndex, StringComparison.Ordinal))
        {
            throw new ClientImportConflictException($"The import {batch.Token} was already committed.");
        }
    }

    private async Task AssignInboxEmailsAsync(List<Client> clients)
    {
        var hasMail = clients.SelectMany(c => c.Communications)
            .Any(c => c.Type is CommunicationTypeEnum.PrivateMail or CommunicationTypeEnum.OfficeMail);
        if (!hasMail)
        {
            return;
        }

        try
        {
            await _emailAssignmentService.AssignInboxEmailsToClientsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assigning inbox e-mails after the client import failed; the import itself is committed");
        }
    }

    private int QueueGeocoding(List<Client> clients)
    {
        var addresses = clients.SelectMany(c => c.Addresses).ToList();
        var queued = 0;

        try
        {
            foreach (var address in addresses)
            {
                if (_geocodingQueue.TryQueue(address.Id))
                {
                    queued++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Queueing addresses for geocoding failed; the import itself is committed");
        }

        var notQueued = addresses.Count - queued;
        if (notQueued > 0)
        {
            _logger.LogWarning("{NotQueued} of {Total} imported addresses were not queued for geocoding (queue full or geocoding disabled)",
                notQueued, addresses.Count);
        }

        return queued;
    }
}
