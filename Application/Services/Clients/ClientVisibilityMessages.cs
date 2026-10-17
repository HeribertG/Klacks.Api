// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Clients;

/// <summary>
/// Not-found text shared by the client write paths, so a hidden client is answered with exactly the text a missing one
/// gets.
/// </summary>
public static class ClientVisibilityMessages
{
    public const string ClientNotFound = "Client with ID {0} not found";
}
