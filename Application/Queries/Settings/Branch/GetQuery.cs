using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Branch;

public record GetQuery(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.Branch>;
