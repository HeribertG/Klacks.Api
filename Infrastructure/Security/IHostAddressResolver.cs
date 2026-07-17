// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net;

namespace Klacks.Api.Infrastructure.Security;

public interface IHostAddressResolver
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct = default);
}
