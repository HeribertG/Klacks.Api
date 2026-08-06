// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Associations;

public record BulkAddGroupItemsCommand(BulkGroupItemRequest Request) : IRequest<BulkGroupItemResponse>;
