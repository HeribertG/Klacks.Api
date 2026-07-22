// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated, cross-prefix parameter aliases used by <see cref="VerifyParamNameBridge"/> when a verify
/// skill's declared parameter name cannot be resolved from a mutation result by name alone. Each entry
/// encodes a domain fact that a pure name heuristic could never guess safely. Keep this table minimal —
/// add a pair only when a real mutation-to-verify pairing needs it, never speculatively.
/// The single current fact: Employee, Customer and ExternEmp records are all the same <c>Client</c>
/// entity, so a verify skill that reads a client by <c>clientId</c> can be sourced from a mutation that
/// returns the created entity id under <c>employeeId</c> (e.g. create_employee -> get_client_details).
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Planning;

public static class VerifyParamAliases
{
    public const string ClientIdParam = "clientId";
    public const string EmployeeIdSource = "employeeId";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SourceKeysByParam =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [ClientIdParam] = new[] { EmployeeIdSource }
        };
}
