using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Commands.GroupVisibilities;

public record BulkGroupVisibilitiesCommand(List<GroupVisibilityResource> List) : IRequest;