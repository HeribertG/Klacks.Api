// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class CompleteOwnAdminSetupCommandHandler : BaseHandler, IRequestHandler<CompleteOwnAdminSetupCommand, bool>
{
    private readonly IAdminSetupGateService _adminSetupGateService;
    private readonly IUserManagementService _userManagementService;
    private readonly AuthMapper _authMapper;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteOwnAdminSetupCommandHandler(
        IAdminSetupGateService adminSetupGateService,
        IUserManagementService userManagementService,
        AuthMapper authMapper,
        IUnitOfWork unitOfWork,
        ILogger<CompleteOwnAdminSetupCommandHandler> logger)
        : base(logger)
    {
        _adminSetupGateService = adminSetupGateService;
        _userManagementService = userManagementService;
        _authMapper = authMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CompleteOwnAdminSetupCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _adminSetupGateService.IsGateActiveAsync())
            {
                throw new ConflictException("Own admin account setup was already completed.");
            }

            var newAdmin = _authMapper.ToAppUser(request.NewAdmin);
            var (registered, identityResult) = await _userManagementService.RegisterUserAsync(newAdmin, request.NewAdmin.Password);

            if (!registered)
            {
                var reason = identityResult != null
                    ? string.Join(", ", identityResult.Errors.Select(e => e.Description))
                    : "Registration failed.";
                _logger.LogWarning("Own-admin setup registration failed for {Email}: {Reason}", request.NewAdmin.Email.MaskEmail(), reason);
                throw new ConflictException(reason);
            }

            var (roleAssigned, roleMessage) = await _userManagementService.ChangeUserRoleAsync(newAdmin.Id, Roles.Admin, true);
            if (!roleAssigned)
            {
                _logger.LogError("Own-admin setup: failed to grant Admin role to {Email}: {Message}", request.NewAdmin.Email.MaskEmail(), roleMessage);
                throw new ConflictException(roleMessage);
            }

            var (deactivated, deactivationMessage) = await _userManagementService.DeactivateUserAsync(
                Guid.Parse(SystemAccounts.SeedAdminUserId), newAdmin.Id);
            if (!deactivated)
            {
                _logger.LogError("Own-admin setup: failed to deactivate the seed admin account: {Message}", deactivationMessage);
                throw new ConflictException(deactivationMessage);
            }

            _logger.LogInformation("Own-admin setup completed: {Email} is now admin, seed admin deactivated", request.NewAdmin.Email.MaskEmail());
            return true;
        });
    }
}
