// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

public class AccountsController : BaseController
{
    private readonly ILogger<AccountsController> _logger;
    private readonly IMediator _mediator;
    private readonly IUsernameGeneratorService _usernameGeneratorService;

    public AccountsController(
        IMediator mediator,
        ILogger<AccountsController> logger,
        IUsernameGeneratorService usernameGeneratorService)
    {
        _mediator = mediator;
        _logger = logger;
        _usernameGeneratorService = usernameGeneratorService;
    }

    [Authorize]
    [HttpPut("ChangePassword")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordResource model)
    {
        _logger.LogInformation("ChangePassword requested for user: {Email}", model.Email);

        var result = await _mediator.Send(new ChangePasswordCommand(model));

        return Ok(result);
    }

    [Authorize]
    [HttpPost("ChangePasswordUser")]
    public async Task<ActionResult> ChangePasswordUser([FromBody] ChangePasswordResource model)
    {
        _logger.LogInformation("ChangePasswordUser requested for user: {Email}", model.Email);

        var result = await _mediator.Send(new ChangePasswordUserCommand(model));

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("ChangeRoleUser")]
    public async Task<ActionResult> ChangeRoleUser([FromBody] ChangeRole changeRole)
    {
        _logger.LogInformation("ChangeRoleUser request received for user: {UserId}", changeRole.UserId);
        var result = await _mediator.Send(new ChangeRoleCommand(changeRole));
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAccountUser(Guid id)
    {
        _logger.LogInformation("DeleteAccountUser request received for user: {Id}", id);
        var result = await _mediator.Send(new DeleteAccountCommand(id));
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult> UpdateAccount([FromBody] UpdateAccountResource model)
    {
        _logger.LogInformation("UpdateAccount request received for user: {UserId}", model.Id);
        var result = await _mediator.Send(new UpdateAccountCommand(model));
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("GenerateUsername")]
    public async Task<ActionResult<string>> GenerateUsername([FromQuery] string firstName, [FromQuery] string lastName)
    {
        _logger.LogInformation("GenerateUsername request for {FirstName} {LastName}", firstName, lastName);
        var username = await _usernameGeneratorService.GenerateUniqueUsernameAsync(firstName, lastName);
        return Ok(username);
    }

    [HttpGet("ValidateToken")]
    public IActionResult ValidateToken()
    {
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResource>>> GetUserList()
    {
        _logger.LogInformation("GetUserList request received");
        var users = await _mediator.Send(new GetUserListQuery());
        _logger.LogInformation("Retrieved {Count} users", users.Count);
        return Ok(users);
    }

    [AllowAnonymous]
    [HttpPost("LoginUser")]
    public async Task<IActionResult> LoginUser([FromBody] LogInResource model)
    {
        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Invalid login request received.");
            return BadRequest("Invalid login data.");
        }

        _logger.LogInformation("Login attempt for user: {Email}", model.Email);

        var result = await _mediator.Send(new LoginUserQuery(model.Email, model.Password));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestResource model)
    {
        _logger.LogInformation("RefreshToken requested.");

        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Invalid refresh token request received.");
            return BadRequest("Invalid data for token update.");
        }

        var result = await _mediator.Send(new RefreshTokenQuery(model));
        
        if (result == null)
        {
            _logger.LogWarning("Token refresh failed - invalid or expired refresh token");
            return Unauthorized("Invalid or expired refresh token");
        }
        
        return Ok(result);
    }

    [HttpPost("RegisterUser")]
    public async Task<ActionResult> RegisterUser([FromBody] RegistrationResource model)
    {
        _logger.LogInformation("RegisterUser request received for: {Email}", model.Email);
        var result = await _mediator.Send(new RegisterUserCommand(model));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("RequestPasswordReset")]
    public async Task<ActionResult> RequestPasswordReset([FromBody] RequestPasswordResetResource model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email))
        {
            _logger.LogWarning("Invalid password reset request received");
            return BadRequest("Email address is required.");
        }

        _logger.LogInformation("Password reset requested for email: {Email}", model.Email);
        
        var result = await _mediator.Send(new RequestPasswordResetCommand(model.Email));
        
        _logger.LogInformation("Password reset request processed for email: {Email}", model.Email);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("ResetPassword")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordResource model)
    {
        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Invalid reset password request received");
            return BadRequest("Invalid data for password reset.");
        }

        _logger.LogInformation("Password reset confirmation requested");
        
        var result = await _mediator.Send(new ResetPasswordCommand(model));
        
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("ValidatePasswordResetToken")]
    public async Task<ActionResult<bool>> ValidatePasswordResetToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Empty token provided for validation");
            return BadRequest("Token is required.");
        }

        _logger.LogInformation("Password reset token validation requested");
        
        var isValid = await _mediator.Send(new ValidatePasswordResetTokenQuery(token));
        
        _logger.LogInformation("Token validation result: {IsValid}", isValid);
        return Ok(isValid);
    }
}
