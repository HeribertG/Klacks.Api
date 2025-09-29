using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AccountsController : BaseController
{
    private readonly ILogger<AccountsController> logger;
    private readonly IMediator mediator;

    public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    [Authorize]
    [HttpPut("ChangePassword")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordResource model)
    {
        this.logger.LogInformation("ChangePassword requested for user: {Email}", model.Email);

        var result = await mediator.Send(new ChangePasswordCommand(model));

        return Ok(result);
    }

    [Authorize]
    [HttpPost("ChangePasswordUser")]
    public async Task<ActionResult> ChangePasswordUser([FromBody] ChangePasswordResource model)
    {
        this.logger.LogInformation("ChangePasswordUser requested for user: {Email}", model.Email);

        var result = await mediator.Send(new ChangePasswordUserCommand(model));

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("ChangeRoleUser")]
    public async Task<ActionResult> ChangeRoleUser([FromBody] ChangeRole changeRole)
    {
        this.logger.LogInformation($"ChangeRoleUser request received for user: {changeRole.UserId}");
        var result = await mediator.Send(new ChangeRoleCommand(changeRole));
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAccountUser(Guid id)
    {
        this.logger.LogInformation($"DeleteAccountUser request received for user: {id}");
        var result = await mediator.Send(new DeleteAccountCommand(id));
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResource>>> GetUserList()
    {
        this.logger.LogInformation("GetUserList request received");
        var users = await mediator.Send(new GetUserListQuery());
        this.logger.LogInformation("Retrieved {Count} users", users.Count);
        return Ok(users);
    }

    [AllowAnonymous]
    [HttpPost("LoginUser")]
    public async Task<IActionResult> LoginUser([FromBody] LogInResource model)
    {
        if (model == null || !ModelState.IsValid)
        {
            this.logger.LogWarning("Invalid login request received.");
            return BadRequest("Invalid login data.");
        }

        this.logger.LogInformation("Login attempt for user: {Email}", model.Email);

        var result = await mediator.Send(new LoginUserQuery(model.Email, model.Password));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestResource model)
    {
        this.logger.LogInformation("RefreshToken requested.");

        if (model == null || !ModelState.IsValid)
        {
            this.logger.LogWarning("Invalid refresh token request received.");
            return BadRequest("Invalid data for token update.");
        }

        var result = await mediator.Send(new RefreshTokenQuery(model));
        
        if (result == null)
        {
            this.logger.LogWarning("Token refresh failed - invalid or expired refresh token");
            return Unauthorized("Invalid or expired refresh token");
        }
        
        return Ok(result);
    }

    [HttpPost("RegisterUser")]
    public async Task<ActionResult> RegisterUser([FromBody] RegistrationResource model)
    {
        this.logger.LogInformation($"RegisterUser request received: {JsonConvert.SerializeObject(model)}");
        var result = await mediator.Send(new RegisterUserCommand(model));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("RequestPasswordReset")]
    public async Task<ActionResult> RequestPasswordReset([FromBody] RequestPasswordResetResource model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email))
        {
            this.logger.LogWarning("Invalid password reset request received");
            return BadRequest("Email address is required.");
        }

        this.logger.LogInformation("Password reset requested for email: {Email}", model.Email);
        
        var result = await mediator.Send(new RequestPasswordResetCommand(model.Email));
        
        this.logger.LogInformation("Password reset request processed for email: {Email}", model.Email);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("ResetPassword")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordResource model)
    {
        if (model == null || !ModelState.IsValid)
        {
            this.logger.LogWarning("Invalid reset password request received");
            return BadRequest("Invalid data for password reset.");
        }

        this.logger.LogInformation("Password reset confirmation requested");
        
        var result = await mediator.Send(new ResetPasswordCommand(model));
        
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("ValidatePasswordResetToken")]
    public async Task<ActionResult<bool>> ValidatePasswordResetToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            this.logger.LogWarning("Empty token provided for validation");
            return BadRequest("Token is required.");
        }

        this.logger.LogInformation("Password reset token validation requested");
        
        var isValid = await mediator.Send(new ValidatePasswordResetTokenQuery(token));
        
        this.logger.LogInformation("Token validation result: {IsValid}", isValid);
        return Ok(isValid);
    }
}
