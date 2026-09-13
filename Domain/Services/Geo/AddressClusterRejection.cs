// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Geo;

public sealed record AddressClusterRejection(
    Guid ClientId,
    string Reason);
