using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Authentification;
using Klacks.Api.Resources.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.Backend;

public class AccountsController : BaseController
{
    private const string MAILFAILURE = "Email Send Failure";
    private const string TRUERESULT = "true";
   

    private readonly ILogger<AccountsController> _logger;
    private readonly IMapper mapper;
    private readonly IAccountRepository repository;

    public AccountsController(IMapper mapper, IAccountRepository repository, ILogger<AccountsController> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        _logger = logger;
    }

    [Authorize]
    [HttpPut("ChangePassword")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordResource model)
    {
        _logger.LogInformation("ChangePassword requested for user: {Email}", model.Email);

        var result = await repository.ChangePassword(model);

        if (result.Success)
        {
            result = await SendEmail(result, model.Title, model.Email, model.Message.Replace("{appName}", model.AppName ?? "Klacks").Replace("{password}", model.Password));

            return Ok(result);
        }

        return BadRequest(result);
    }

    [Authorize]
    [HttpPost("ChangePasswordUser")]
    public async Task<ActionResult> ChangePasswordUser([FromBody] ChangePasswordResource model)
    {
        _logger.LogInformation("ChangePasswordUser requested for user: {Email}", model.Email);

        var result = await repository.ChangePasswordUser(model);

        if (result != null && result.Success)
        {
            result = await SendEmail(result, model.Title, model.Email, model.Message.Replace("{appName}", model.AppName ?? "Klacks").Replace("{password}", model.Password));
            return Ok(result);
        }

        return BadRequest(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("ChangeRoleUser")]
    public async Task<ActionResult> ChangeRoleUser([FromBody] ChangeRole changeRole)
    {
        _logger.LogInformation($"ChangeRoleUser request received for user: {changeRole.UserId}");
        try
        {
            var result = await repository.ChangeRoleUser(changeRole);
            if (result != null && result.Success)
            {
                return Ok(result);
            }

            _logger.LogWarning("Change role failed for user: {UserId}", changeRole.UserId);
            return Conflict($"Change role failed for user: {changeRole.UserId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while changing role for user: {UserId}", changeRole.UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAccountUser(Guid id)
    {
        _logger.LogInformation($"DeleteAccountUser request received for user: {id}");
        try
        {
            var result = await repository.DeleteAccountUser(id);

            if (result != null && result.Success)
            {
                _logger.LogInformation("Account deletion successful for user: {UserId}", id);
                return Ok(result);
            }

            _logger.LogWarning("Account deletion failed for user: {UserId}, Reason: {Reason}", id, result?.Messages ?? "Unknown");
            return NotFound($"User with ID {id} not found or could not be deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting account for user: {UserId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResource>>> GetUserList()
    {
        _logger.LogInformation("GetUserList request received");
        try
        {
            var users = await repository.GetUserList();
            _logger.LogInformation("Retrieved {Count} users", users.Count);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching user list");
            return StatusCode(500, "An error has occurred. Please try again later.");
        }
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

        try
        {
            var result = await repository.LogInUser(model.Email, model.Password);

            if (result != null && result.Success)
            {
                var response = new TokenResource
                {
                    Success = true,
                    Token = result.Token,
                    Username = result.UserName,
                    FirstName = result.FirstName,
                    Name = result.Name,
                    Id = result.Id,
                    ExpTime = result.Expires,
                    IsAdmin = result.IsAdmin,
                    IsAuthorised = result.IsAuthorised,
                    RefreshToken = result.RefreshToken,
                    Version = new MyVersion().Get(),
                    Subject = model.Email
                };

                _logger.LogInformation("Login successful for user: {Email}", model.Email);
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("Login failed for user: {Email}", model.Email);
                return Unauthorized("Invalid e-mail address or password.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during user registration: {Email}", model.Email);
            return StatusCode(500, "An error has occurred. Please try again later.");
        }
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

        try
        {
            var result = await repository.RefreshToken(model);

            if (result != null && result.Success)
            {
                var response = new TokenResource
                {
                    Success = true,
                    Token = result.Token,
                    Username = result.UserName,
                    FirstName = result.FirstName,
                    Name = result.Name,
                    Id = result.Id,
                    ExpTime = result.Expires,
                    IsAdmin = result.IsAdmin,
                    IsAuthorised = result.IsAuthorised,
                    RefreshToken = result.RefreshToken,
                    Version = new MyVersion().Get()
                };

                _logger.LogInformation("Token refresh successful for user: {UserId}", result.Id);
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("Token refresh failed: Registration has expired.");
                return Unauthorized("Your registration has expired.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during token refresh.");
            return StatusCode(500, "An error has occurred. Please try again later.");
        }
    }


    [HttpPost("RegisterUser")]
    public async Task<ActionResult> RegisterUser([FromBody] RegistrationResource model)
    {
        _logger.LogInformation($"RegisterUser request received: {JsonConvert.SerializeObject(model)}");
        try
        {
            var userIdentity = mapper.Map<AppUser>(model);

            var result = await repository.RegisterUser(userIdentity, model.Password);

            if (result != null && result.Success)
            {
                _logger.LogInformation("User registration successful: {Email}", model.Email);
                return Ok(result);
            }

            _logger.LogWarning("User registration failed: {Email}, Reason: {Reason}", model.Email, result?.Message ?? "Unknow");
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during user registration: {Email}", model.Email);
            return StatusCode(500, "An error has occurred during user registration.");
        }
    }

    private async Task<AuthenticatedResult> SendEmail(AuthenticatedResult result, string title, string email, string message)
    {
        var mailResult = await repository.SendEmail(title, email, message);

        result.MailSuccess = false;

        if (!string.IsNullOrEmpty(mailResult))
        {
            result.MailSuccess = string.Compare(mailResult, TRUERESULT) == 0 ? true : false;
        }

        if (!result.MailSuccess)
        {
            repository.SetModelError(result, MAILFAILURE, mailResult);
        }

        return result;
    }
}
