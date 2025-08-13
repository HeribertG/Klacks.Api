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

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [Authorize]
    [HttpPost("ChangePasswordUser")]
    public async Task<ActionResult> ChangePasswordUser([FromBody] ChangePasswordResource model)
    {
        this.logger.LogInformation("ChangePasswordUser requested for user: {Email}", model.Email);

        var result = await mediator.Send(new ChangePasswordUserCommand(model));

        if (result != null && result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("ChangeRoleUser")]
    public async Task<ActionResult> ChangeRoleUser([FromBody] ChangeRole changeRole)
    {
        this.logger.LogInformation($"ChangeRoleUser request received for user: {changeRole.UserId}");
        try
        {
            var result = await mediator.Send(new ChangeRoleCommand(changeRole));
            if (result != null && result.Success)
            {
                return Ok(result);
            }

            this.logger.LogWarning("Change role failed for user: {UserId}", changeRole.UserId);
            return Conflict($"Change role failed for user: {changeRole.UserId}");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception occurred while changing role for user: {UserId}", changeRole.UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAccountUser(Guid id)
    {
        this.logger.LogInformation($"DeleteAccountUser request received for user: {id}");
        try
        {
            var result = await mediator.Send(new DeleteAccountCommand(id));

            if (result != null && result.Success)
            {
                this.logger.LogInformation("Account deletion successful for user: {UserId}", id);
                return Ok(result);
            }

            this.logger.LogWarning("Account deletion failed for user: {UserId}, Reason: {Reason}", id, result?.Messages ?? "Unknown");
            return NotFound($"User with ID {id} not found or could not be deleted.");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception occurred while deleting account for user: {UserId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResource>>> GetUserList()
    {
        this.logger.LogInformation("GetUserList request received");
        try
        {
            var users = await mediator.Send(new GetUserListQuery());
            this.logger.LogInformation("Retrieved {Count} users", users.Count);
            return Ok(users);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception occurred while fetching user list");
            return StatusCode(500, "An error has occurred. Please try again later.");
        }
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

        try
        {
            var result = await mediator.Send(new LoginUserQuery(model.Email, model.Password));

            if (result != null)
            {
                this.logger.LogInformation("Login successful for user: {Email}", model.Email);
                return Ok(result);
            }
            else
            {
                this.logger.LogWarning("Login failed for user: {Email}", model.Email);
                return Unauthorized("Invalid e-mail address or password.");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception occurred during user login: {Email}", model.Email);
            return StatusCode(500, "An error has occurred. Please try again later.");
        }
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

        try
        {
            var result = await mediator.Send(new RefreshTokenQuery(model));

            if (result != null)
            {
                this.logger.LogInformation("Token refresh successful for user: {UserId}", result.Id);
                return Ok(result);
            }
            else
            {
                this.logger.LogWarning("Token refresh failed: Registration has expired.");
                return Unauthorized("Your registration has expired.");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception occurred during token refresh.");
            return StatusCode(500, "An error has occurred. Please try again later.");
        }
    }

    [HttpPost("RegisterUser")]
    public async Task<ActionResult> RegisterUser([FromBody] RegistrationResource model)
    {
        this.logger.LogInformation($"RegisterUser request received: {JsonConvert.SerializeObject(model)}");
        try
        {
            var result = await mediator.Send(new RegisterUserCommand(model));

            if (result != null && result.Success)
            {
                this.logger.LogInformation("User registration successful: {Email}", model.Email);
                return Ok(result);
            }

            this.logger.LogWarning("User registration failed: {Email}, Reason: {Reason}", model.Email, result?.Message ?? "Unknown");
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception occurred during user registration: {Email}", model.Email);
            return StatusCode(500, "An error has occurred during user registration.");
        }
    }
}
