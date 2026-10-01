// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Emails a client's schedule report (PDF) to the client's private or office address and clears the tracked
/// schedule changes afterwards. A client outside the caller's group visibility is answered exactly like a
/// client without an email address; nothing is sent.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>

using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class SendScheduleReportCommandHandler : BaseHandler,
    IRequestHandler<SendScheduleReportCommand, SendScheduleReportResponse>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IScheduleEmailService _scheduleEmailService;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public SendScheduleReportCommandHandler(
        ICommunicationRepository communicationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IScheduleEmailService scheduleEmailService,
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<SendScheduleReportCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleEmailService = scheduleEmailService;
        _scheduleChangeTracker = scheduleChangeTracker;
    }

    public async Task<SendScheduleReportResponse> Handle(
        SendScheduleReportCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var communications = await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken)
                ? await _communicationRepository.GetClient(request.ClientId)
                : [];

            var email = communications.FirstOrDefault(c => c.Type == CommunicationTypeEnum.PrivateMail)
                ?? communications.FirstOrDefault(c => c.Type == CommunicationTypeEnum.OfficeMail);

            if (email == null || string.IsNullOrWhiteSpace(email.Value))
            {
                return new SendScheduleReportResponse
                {
                    Success = false,
                    ErrorMessage = "No email address found for client"
                };
            }

            var result = await _scheduleEmailService.SendScheduleEmailAsync(
                email.Value,
                request.ClientName,
                request.StartDate,
                request.EndDate,
                request.PdfData,
                request.FileName);

            if (result)
            {
                try
                {
                    var startDate = DateOnly.ParseExact(request.StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    var endDate = DateOnly.ParseExact(request.EndDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    await _scheduleChangeTracker.ClearChangesAsync(request.ClientId, startDate, endDate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear schedule changes after sending report for client {ClientId}", request.ClientId);
                }

                return new SendScheduleReportResponse
                {
                    Success = true,
                    ClientEmail = email.Value
                };
            }

            return new SendScheduleReportResponse
            {
                Success = false,
                ErrorMessage = "Failed to send schedule email",
                ClientEmail = email.Value
            };
        },
        "sending schedule report email",
        new { request.ClientId, request.ClientName });
    }
}
