using MediatR;
using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Application.Queries.Clients;

public sealed record FindListQuery(string? Company = null, string? Name = null, string? FirstName = null) : IRequest<IEnumerable<Client>>;
