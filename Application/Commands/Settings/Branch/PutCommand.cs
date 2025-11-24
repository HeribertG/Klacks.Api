using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Branch;

public record PutCommand(Klacks.Api.Domain.Models.Settings.Branch model) : IRequest<Klacks.Api.Domain.Models.Settings.Branch>;
