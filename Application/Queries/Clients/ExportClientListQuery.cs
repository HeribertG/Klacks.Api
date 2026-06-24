// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Clients;

public record ExportClientListQuery(ExportClientRequest Request) : IRequest<List<ExportClientItemDto>>;
