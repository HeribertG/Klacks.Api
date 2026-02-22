// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Reports;

public record SendScheduleReportCommand(
    Guid ClientId,
    string ClientName,
    string StartDate,
    string EndDate,
    byte[] PdfData,
    string FileName) : IRequest<SendScheduleReportResponse>;
