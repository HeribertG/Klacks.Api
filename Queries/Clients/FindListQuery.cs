using MediatR;
using Klacks_api.Models.Staffs;

namespace Klacks_api.Queries.Clients;

public sealed record FindListQuery(string? Company = null, string? Name = null, string? FirstName = null) : IRequest<IEnumerable<Client>>;
