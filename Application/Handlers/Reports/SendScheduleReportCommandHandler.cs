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
    private readonly IScheduleEmailService _scheduleEmailService;

    public SendScheduleReportCommandHandler(
        ICommunicationRepository communicationRepository,
        IScheduleEmailService scheduleEmailService,
        ILogger<SendScheduleReportCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _scheduleEmailService = scheduleEmailService;
    }

    public async Task<SendScheduleReportResponse> Handle(
        SendScheduleReportCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var communications = await _communicationRepository.GetClient(request.ClientId);

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
