// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

public record UnassignedCustomer(Guid ClientId, string ClientName, string Reason);
