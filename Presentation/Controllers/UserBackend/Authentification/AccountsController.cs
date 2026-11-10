// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Security.Claims;
using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Authentication;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

public class AccountsController : BaseController
{
    private const string InvalidRefreshTokenMessage = "Invalid or expired refresh token";

    private readonly ILogger<AccountsController> _logger;
    private readonly IMediator _mediator;
    private readonly IUsernameGeneratorService _usernameGeneratorService;
    private readonly IRefreshTokenCookieService _refreshTokenCookie;
    private readonly IAccessTokenCookieService _accessTokenCookie;

    public AccountsController(
        IMediator mediator,
        ILogger<AccountsController> logger,
        IUsernameGeneratorService usernameGeneratorService,
        IRefreshTokenCookieService refreshTokenCookie,
        IAccessTokenCookieService accessTokenCookie)
    {
        _mediator = mediator;
        _logger = logger;
        _usernameGeneratorService = usernameGeneratorService;
        _refreshTokenCookie = refreshTokenCookie;
        _accessTokenCookie = accessTokenCookie;
    }

    [BlockedOnPlayground]
    [Authorize]
    [ExemptFromAdminSetupGate]
    [EnableRateLimiting(RateLimitingPolicies.PasswordChange)]
    [RefreshCookieRequestGuard]
    [HttpPost("ChangePasswordUser")]
    public async Task<ActionResult> ChangePasswordUser([FromBody] ChangePasswordResource model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        _logger.LogInformation("ChangePasswordUser requested for user: {UserId}", userId);

        var result = await _mediator.Send(new ChangePasswordUserCommand(userId, model));
        _refreshTokenCookie.Write(HttpContext, result.RefreshToken);
        _accessTokenCookie.Write(HttpContext, result.Token);

        return Ok(result);
    }

    [BlockedOnPlayground]
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("ChangeRoleUser")]
    public async Task<ActionResult> ChangeRoleUser([FromBody] ChangeRole changeRole)
    {
        _logger.LogInformation("ChangeRoleUser request received for user: {UserId}", changeRole.UserId.ForLog());
        var result = await _mediator.Send(new ChangeRoleCommand(changeRole));
        return Ok(result);
    }

    [BlockedOnPlayground]
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id}/Deactivate")]
    public async Task<ActionResult> DeactivateAccountUser(Guid id)
    {
        _logger.LogInformation("DeactivateAccountUser request received for user: {Id}", id);
        var result = await _mediator.Send(new DeactivateAccountCommand(id));
        return Ok(result);
    }

    [BlockedOnPlayground]
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id}/Reactivate")]
    public async Task<ActionResult> ReactivateAccountUser(Guid id)
    {
        _logger.LogInformation("ReactivateAccountUser request received for user: {Id}", id);
        var result = await _mediator.Send(new ReactivateAccountCommand(id));
        return Ok(result);
    }

    [BlockedOnPlayground]
    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<ActionResult> UpdateAccount([FromBody] UpdateAccountResource model)
    {
        _logger.LogInformation("UpdateAccount request received for user: {UserId}", model.Id);
        var result = await _mediator.Send(new UpdateAccountCommand(model));
        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("Reorder")]
    public async Task<ActionResult<HttpResultResource>> Reorder([FromBody] ReorderUsersCommand command)
    {
        _logger.LogInformation("Reorder users request received for {Count} users", command.OrderedUserIds.Count);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("GenerateUsername")]
    public async Task<ActionResult<string>> GenerateUsername([FromQuery] string firstName, [FromQuery] string lastName)
    {
        _logger.LogInformation("GenerateUsername request for {FirstName} {LastName}", firstName.ForLog(), lastName.ForLog());
        var username = await _usernameGeneratorService.GenerateUniqueUsernameAsync(firstName, lastName);
        return Ok(username);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ExemptFromAdminSetupGate]
    [HttpGet("ValidateToken")]
    public IActionResult ValidateToken()
    {
        return Ok();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResource>>> GetUserList()
    {
        _logger.LogInformation("GetUserList request received");
        var users = await _mediator.Send(new GetUserListQuery());
        _logger.LogInformation("Retrieved {Count} users", users.Count);
        return Ok(users);
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.Login)]
    [HttpPost("LoginUser")]
    public async Task<IActionResult> LoginUser([FromBody] LogInResource model)
    {
        if (model == null || !ModelState.IsValid)
        {
            _logger.LogWarning("Invalid login request received.");
            return BadRequest("Invalid login data.");
        }

        _logger.LogInformation("Login attempt for user: {Email}", model.Email.MaskEmail());

        var result = await _mediator.Send(new LoginUserQuery(
            model.Email, model.Password, DirectLoopbackRequest.IsDirectLoopback(HttpContext)));
        _refreshTokenCookie.Write(HttpContext, result.RefreshToken);
        _accessTokenCookie.Write(HttpContext, result.Token);
        return Ok(result);
    }

    [AllowAnonymous]
    [ExemptFromAdminSetupGate]
    [EnableRateLimiting(RateLimitingPolicies.RefreshToken)]
    [RefreshCookieRequestGuard]
    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshRequestResource? model)
    {
        _logger.LogInformation("RefreshToken requested.");

        var body = model ?? new RefreshRequestResource();
        var cookieToken = _refreshTokenCookie.Read(Request);
        if (string.IsNullOrEmpty(body.RefreshToken) && string.IsNullOrEmpty(cookieToken))
        {
            _logger.LogWarning("Token refresh without a refresh token in body or cookie");
            return Unauthorized(InvalidRefreshTokenMessage);
        }

        var result = await _mediator.Send(new RefreshTokenQuery(body, cookieToken));

        if (result == null)
        {
            _logger.LogWarning("Token refresh failed - invalid or expired refresh token");
            return Unauthorized(InvalidRefreshTokenMessage);
        }

        _refreshTokenCookie.Write(HttpContext, result.RefreshToken);
        _accessTokenCookie.Write(HttpContext, result.Token);
        return Ok(result);
    }

    [AllowAnonymous]
    [ExemptFromAdminSetupGate]
    [EnableRateLimiting(RateLimitingPolicies.RefreshToken)]
    [RefreshCookieRequestGuard]
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshRequestResource? model)
    {
        _logger.LogInformation("Logout requested.");

        var refreshToken = _refreshTokenCookie.Read(Request) ?? NullIfEmpty(model?.RefreshToken);
        await _mediator.Send(new LogoutCommand(refreshToken));
        _refreshTokenCookie.Delete(HttpContext);
        _accessTokenCookie.Delete(HttpContext);

        return Ok();
    }

    [BlockedOnPlayground]
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("RegisterUser")]
    public async Task<ActionResult> RegisterUser([FromBody] RegistrationResource model)
    {
        _logger.LogInformation("RegisterUser request received for: {Email}", model.Email.MaskEmail());
        var result = await _mediator.Send(new RegisterUserCommand(model));
        return Ok(result);
    }

    [BlockedOnPlayground]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.PasswordReset)]
    [HttpPost("RequestPasswordReset")]
    public async Task<ActionResult> RequestPasswordReset([FromBody] RequestPasswordResetResource model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email))
        {
            _logger.LogWarning("Invalid password reset request received");
            return BadRequest("Email address is required.");
        }

        _logger.LogInformation("Password reset requested for email: {Email}", model.Email.MaskEmail());

        var result = await _mediator.Send(new RequestPasswordResetCommand(model.Email));

        _logger.LogInformation("Password reset request processed for email: {Email}", model.Email.MaskEmail());
        return Ok(result);
    }

    [BlockedOnPlayground]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.PasswordReset)]
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
        if (result.Success)
        {
            _refreshTokenCookie.Delete(HttpContext);
            _accessTokenCookie.Delete(HttpContext);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.PasswordReset)]
    [HttpPost("ValidatePasswordResetToken")]
    public async Task<ActionResult<bool>> ValidatePasswordResetToken([FromBody] ValidatePasswordResetTokenResource model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Token))
        {
            _logger.LogWarning("Empty token provided for validation");
            return BadRequest("Token is required.");
        }

        _logger.LogInformation("Password reset token validation requested");

        var isValid = await _mediator.Send(new ValidatePasswordResetTokenQuery(model.Token));
        
        _logger.LogInformation("Token validation result: {IsValid}", isValid);
        return Ok(isValid);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
