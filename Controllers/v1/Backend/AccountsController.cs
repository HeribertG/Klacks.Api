using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Models.Authentification;
using Klacks_api.Resources.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks_api.Controllers.V1.Backend;

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

  [HttpPut("ChangePassword")]
  public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordResource model)
  {
    _logger.LogInformation($"ChangePassword ChangePasswordResource Resource: {JsonConvert.SerializeObject(model)}");

    var result = await repository.ChangePassword(model);

    if (result.Success)
    {
      result = await SendEmail(result, model.Title, model.Email, model.Message.Replace("{appName}", model.AppName ?? "Klacks").Replace("{password}", model.Password));

      return Ok(result);
    }

    return BadRequest(result);
  }

  [HttpPost("ChangePasswordUser")]
  public async Task<ActionResult> ChangePasswordUser([FromBody] ChangePasswordResource model)
  {
    _logger.LogInformation($"ChangePasswordUser ChangePasswordResource Resource: {JsonConvert.SerializeObject(model)}");

    var result = await repository.ChangePasswordUser(model);

    if (result.Success)
    {
      result = await SendEmail(result, model.Title, model.Email, model.Message.Replace("{appName}", model.AppName ?? "Klacks").Replace("{password}", model.Password));
      return Ok(result);
    }

    return BadRequest(result);
  }

  [HttpPut("ChangeRoleUser")]
  public async Task<ActionResult> ChangeRoleUser([FromBody] ChangeRole changeRole)
  {
    _logger.LogInformation($"ChangeRoleUser request received for user: {changeRole.UserId}");
    try
    {
      var res = await repository.ChangeRoleUser(changeRole);
      if (res != null)
      {
        return Ok(res);
      }

      _logger.LogWarning("Change role failed for user: {UserId}", changeRole.UserId);
      return BadRequest();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Exception occurred while changing role for user: {UserId}", changeRole.UserId);
      throw;
    }
  }

  [HttpDelete("{id}")]
  public async Task<ActionResult> DeleteAccountUser(Guid id)
  {
    _logger.LogInformation($"DeleteAccountUser request received for user: {id}");
    try
    {
      var res = await repository.DeleteAccountUser(id);

      if (res.Success)
      {
        _logger.LogInformation("Account deletion successful for user: {UserId}", id);
        return Ok(res);
      }

      _logger.LogWarning("Account deletion failed for user: {UserId}, Reason: {Reason}", id, res.Messages);
      return BadRequest(res);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Exception occurred while deleting account for user: {UserId}", id);
      throw;
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
      throw;
    }
  }

  [AllowAnonymous]
  [HttpPost("LoginUser")]
  public async Task<ActionResult<TokenResource>> LoginUser([FromBody] LogInResource model)
  {
    _logger.LogInformation($"LoginUser Resource: {JsonConvert.SerializeObject(model)}");

    var response = new TokenResource();
    AuthenticatedResult result;

    try
    {
      response.Success = false;
      response.ErrorMessage = string.Empty;
      response.Subject = model.Email;

      result = await repository.LogInUser(model.Email, model.Password);

      if (result.Success)
      {
        var version = new MyVersion();
        var currentVersion = version.Get();

        response.Success = true;
        response.Token = result.Token;
        response.Username = result.UserName;
        response.FirstName = result.FirstName;
        response.Name = result.Name;
        response.Id = result.Id;
        response.ExpTime = result.Expires;
        response.IsAdmin = result.IsAdmin;
        response.IsAuthorised = result.IsAuthorised;
        response.RefreshToken = result.RefreshToken;

        response.Version = currentVersion;

        return Ok(response);
      }
    }
    catch (Exception ex)
    {
      response.ErrorMessage = ex.Message;
      throw;
    }

    return Ok(result.ModelState);
  }

  [AllowAnonymous]
  [HttpPost("RefreshToken")]
  public async Task<ActionResult<TokenResource>> RefreshToken([FromBody] RefreshRequestResource model)
  {
    _logger.LogInformation($"RefreshToken Resource: {JsonConvert.SerializeObject(model)}");

    var response = new TokenResource();
    AuthenticatedResult result;

    try
    {
      response.Success = false;
      response.ErrorMessage = string.Empty;

      result = await repository.RefreshToken(model);

      if (result != null && result.Success)
      {
        var version = new MyVersion();
        var currentVersion = version.Get();

        response.Success = true;
        response.Token = result.Token;
        response.Username = result.UserName;
        response.FirstName = result.FirstName;
        response.Name = result.Name;
        response.Id = result.Id;
        response.ExpTime = result.Expires;
        response.IsAdmin = result.IsAdmin;
        response.IsAuthorised = result.IsAuthorised;

        response.Version = currentVersion;

        return Ok(response);
      }
      else
      {
        response = new TokenResource
        {
          ErrorMessage = "Ihre Anmeldung ist abgelaufen",
          Success = false,
        };

        return Ok(response);
      }
    }
    catch (Exception ex)
    {
      response.Success = false;
      response.ErrorMessage = ex.Message;
    }

    return Ok(response);
  }

  [HttpPost("RegisterUser")]
  public async Task<ActionResult> RegisterUser([FromBody] RegistrationResource model)
  {
    _logger.LogInformation($"RegisterUser request received: {JsonConvert.SerializeObject(model)}");
    try
    {
      var userIdentity = mapper.Map<AppUser>(model);

      var result = await repository.RegisterUser(userIdentity, model.Password);

      if (result.Success)
      {
        _logger.LogInformation("User registration successful: {Email}", model.Email);
        return Ok(result);
      }

      _logger.LogWarning("User registration failed: {Email}, Reason: {Reason}", model.Email, result.Message);
      return BadRequest(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Exception occurred during user registration: {Email}", model.Email);
      throw;
    }
  }

  private async Task<AuthenticatedResult> SendEmail(AuthenticatedResult result, string title, string email, string message)
  {
    var mailResult = await repository.SendEmail(title, email, message);

    result.MailSuccess = string.Compare(mailResult, TRUERESULT) == 0 ? true : false;
    if (string.Compare(mailResult, TRUERESULT) != 0)
    {
      repository.SetModelError(result, MAILFAILURE, mailResult);
    }

    return result;
  }
}
