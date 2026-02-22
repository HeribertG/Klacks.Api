// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Reports;

public record CreateReportTemplateCommand(ReportTemplate Template) : IRequest<ReportTemplate>;
