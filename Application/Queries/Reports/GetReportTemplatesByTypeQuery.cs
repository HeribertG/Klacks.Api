// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Reports;

public record GetReportTemplatesByTypeQuery(ReportType Type) : IRequest<IEnumerable<ReportTemplate>>;
