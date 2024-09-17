using Klacks_api.Datas;
using Klacks_api.Helper;
using Klacks_api.Helper.Email;
using Klacks_api.Interfaces;
using Klacks_api.Models.Authentification;
using Klacks_api.Resources;
using Klacks_api.Resources.Registrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Klacks_api.Repositories;

public class AccountRepository : IAccountRepository
{
  private const string NotApplicable = "N/A";
  private readonly DataBaseContext appDbContext;
  private readonly JwtSettings jwtSettings;
  private readonly ITokenService tokenService;
  private readonly UserManager<AppUser> userManager;

  public AccountRepository(
                           DataBaseContext appDbContext,
                           UserManager<AppUser> userManager,
                           JwtSettings jwtSettings,
                           ITokenService tokenService)
  {
    this.appDbContext = appDbContext;
    this.userManager = userManager;
    this.jwtSettings = jwtSettings;
    this.tokenService = tokenService;
  }

  public async Task<AuthenticatedResult> ChangePassword(ChangePasswordResource model)
  {
    var authenticatedResult = new AuthenticatedResult { Success = false };

    var existingUser = await userManager.FindByEmailAsync(model.Email);

    if (existingUser == null)
    {
      SetModelError(authenticatedResult, "user does not exist", "User with this email Address do not  exist");

      return authenticatedResult;
    }

    try
    {
      var removePassResult = await userManager.RemovePasswordAsync(existingUser);

      if (!removePassResult.Succeeded)
      {
        authenticatedResult.ModelState = AddErrorsToModelState(removePassResult, authenticatedResult.ModelState!);
        return authenticatedResult;
      }
    }
    catch (Exception e)
    {
      authenticatedResult.Success = false;

      authenticatedResult.ModelState!.TryAddModelError("Change Password Failure", e.Message);
      return authenticatedResult;
    }

    try
    {
      var addPassResult = await userManager.AddPasswordAsync(existingUser, model.Password);

      if (!addPassResult.Succeeded)
      {
        authenticatedResult.ModelState = AddErrorsToModelState(addPassResult, authenticatedResult.ModelState!);
      }
    }
    catch (Exception e)
    {
      SetModelError(authenticatedResult, "Change Password Failure", e.Message);
    }

    authenticatedResult.Success = true;
    return authenticatedResult;
  }

  public async Task<AuthenticatedResult> ChangePasswordUser(ChangePasswordResource model)
  {
    var authenticatedResult = new AuthenticatedResult { Success = false };

    var existingUser = await userManager.FindByEmailAsync(model.Email);

    if (existingUser == null)
    {
      SetModelError(authenticatedResult, "The user does not exist", "There is no user with this e-mail address.");
      return authenticatedResult;
    }

    try
    {
      var resetPassResult = await userManager.ChangePasswordAsync(existingUser, model.OldPassword, model.Password);
      if (resetPassResult.Succeeded)
      {
        authenticatedResult.Success = true;
      }
      else
      {
        authenticatedResult.ModelState = AddErrorsToModelState(resetPassResult, authenticatedResult.ModelState ?? new ModelStateDictionary());
      }
    }
    catch (Exception e)
    {
      SetModelError(authenticatedResult, e.Message, string.Empty);
    }

    return authenticatedResult;
  }

  /// <summary>
  /// Changes the role of a user.
  /// </summary>
  /// <param name="editUserRole">The information on the role change.</param>
  public async Task<HttpResultResource> ChangeRoleUser(ChangeRole editUserRole)
  {
    var res = new HttpResultResource();
    res.Success = false;
    var user = await userManager.FindByIdAsync(editUserRole.UserId);
    if (user != null)
    {
      IdentityResult? result = null;

      if (editUserRole.IsSelected && !(await userManager.IsInRoleAsync(user, editUserRole.RoleName)))
      {
        result = await userManager.AddToRoleAsync(user, editUserRole.RoleName);
      }
      else if (!editUserRole.IsSelected && await userManager.IsInRoleAsync(user, editUserRole.RoleName))
      {
        result = await userManager.RemoveFromRoleAsync(user, editUserRole.RoleName);
      }
      else
      {
        res.Success = true;
        res.Messages = "No change to the role required.";
        return res;
      }

      if (result == null || result.Succeeded)
      {
        res.Success = true;
        return res;
      }

      var errorMessageBuilder = new StringBuilder();
      foreach (var error in result.Errors)
      {
        errorMessageBuilder.AppendLine(error.Description);
      }

      res.Messages = errorMessageBuilder.ToString();
    }

    return res;
  }

  public async Task<HttpResultResource> DeleteAccountUser(Guid id)
  {
    var res = new HttpResultResource();
    try
    {
      var user = await appDbContext.AppUser.SingleOrDefaultAsync(x => x.Id == id.ToString());
      appDbContext.Remove(user!);
      await appDbContext.SaveChangesAsync();
      res.Success = true;
    }
    catch (Exception e)
    {
      res.Success = false;
      res.Messages = e.Message;
    }

    return res;
  }

  public async Task<List<UserResource>> GetUserList()
  {
    var users = await appDbContext.AppUser.ToListAsync();

    var userResources = new List<UserResource>(users.Count);
    var usersInAuthorisedRole = await userManager.GetUsersInRoleAsync("Authorised");
    var usersInAdminRole = await userManager.GetUsersInRoleAsync("Admin");

    foreach (var user in users)
    {
      var userResource = new UserResource
      {
        Id = user.Id,
        UserName = user.UserName ?? NotApplicable,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email ?? NotApplicable,
        IsAuthorised = usersInAuthorisedRole.Contains(user),
        IsAdmin = usersInAdminRole.Contains(user),
      };

      userResources.Add(userResource);
    }

    return userResources;
  }

  public async Task<AuthenticatedResult> LogInUser(string email, string password)
  {
    var authenticatedResult = new AuthenticatedResult { Success = false };
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
      SetModelError(authenticatedResult, "Login failed", "The e-mail and password must not be empty.");
      return authenticatedResult;
    }

    try
    {
      var user = await userManager.FindByEmailAsync(email);
      if (user != null && await userManager.CheckPasswordAsync(user, password))
      {
        return await GenerateAuthentication(user);
      }

      SetModelError(authenticatedResult, "Login failed", "The login details are not correct.");
    }
    catch (Exception ex)
    {
      SetModelError(authenticatedResult, "Login failed", ex.Message);
    }

    return authenticatedResult;
  }

  public async Task<AuthenticatedResult> RefreshToken(RefreshRequestResource model)
  {
    var authenticatedResult = new AuthenticatedResult { Success = false };

    if (model == null)
    {
      SetModelError(authenticatedResult, "RefreshTokenError", "Refresh request model cannot be null.");
      return authenticatedResult;
    }

    var user = GetUserFromAccessToken(model.Token);
    if (user == null)
    {
      SetModelError(authenticatedResult, "RefreshTokenError", "Token invalid or expired.");
      return authenticatedResult;
    }

    try
    {
      if (await ValidateRefreshTokenAsync(user, model.RefreshToken))
      {
        return await GenerateAuthentication(user, false);
      }
    }
    catch (Exception)
    {
      SetModelError(authenticatedResult, "RefreshTokenError", "An error occurred while refreshing the token.");
      return authenticatedResult;
    }

    SetModelError(authenticatedResult, "RefreshTokenError", "Invalid refresh token.");
    return authenticatedResult;
  }

  /// <summary>
  /// Registers a new user.
  /// </summary>
  /// <param name="user">Username of the user.</param>
  /// <param name="password">His password.</param>
  public async Task<AuthenticatedResult> RegisterUser(AppUser user, string password)
  {
    var authenticatedResult = new AuthenticatedResult { Success = false };

    if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.UserName))
    {
      SetModelError(authenticatedResult, "Incorrect or missing user information", "Username or email missing");

      return authenticatedResult;
    }

    var existingUser = await userManager.FindByEmailAsync(user.Email!);

    if (existingUser != null)
    {
      authenticatedResult.Success = false;
      SetModelError(authenticatedResult, "user exists", "User with this email address already exists");

      return authenticatedResult;
    }

    user.UserName = FormatHelper.ReplaceUmlaud(user.UserName!);
    var result = await userManager.CreateAsync(user, password);

    if (!result.Succeeded)
    {
      authenticatedResult.Success = false;

      authenticatedResult.ModelState = AddErrorsToModelState(result, authenticatedResult.ModelState!);
      return authenticatedResult;
    }

    var expires = SetExpires();
    authenticatedResult = await SetAuthenticatedResult(authenticatedResult, user, expires);

    await appDbContext.SaveChangesAsync();

    return authenticatedResult!;
  }

  public Task<AuthenticatedResult> ResetPassword(ResetPasswordResource data)
  {
    throw new NotImplementedException();
  }

  public Task<string> SendEmail(string title, string email, string message)
  {
    var mail = new MsgEMail(appDbContext);
    return Task.FromResult(mail.SendMail(email, title, message));
  }

  public void SetModelError(AuthenticatedResult model, string key, string message)
  {
    if (model.ModelState == null)
    {
      model.ModelState = new ModelStateDictionary();
    }

    model.ModelState.TryAddModelError(key, message);
  }

  public async Task<bool> ValidateRefreshTokenAsync(AppUser user, string refreshToken)
  {
    var storedRefreshToken = await appDbContext.RefreshToken
      .Where(x => x.Token == refreshToken && x.AspNetUsersId == user.Id)
      .OrderByDescending(x => x.ExpiryDate)
      .FirstOrDefaultAsync();

    return storedRefreshToken?.ExpiryDate > DateTime.UtcNow;
  }

  private ModelStateDictionary AddErrorsToModelState(IdentityResult identityResult, ModelStateDictionary modelState)
  {
    if (modelState == null)
    {
      modelState = new ModelStateDictionary();
    }

    foreach (var e in identityResult.Errors)
    {
      modelState.TryAddModelError(e.Code, e.Description);
    }

    return modelState;
  }

  private async Task<AuthenticatedResult> GenerateAuthentication(AppUser user, bool withRefreshToken = true)
  {
    var authenticatedResult = new AuthenticatedResult { Success = false };
    var expires = SetExpires();

    if (withRefreshToken)
    {
      var refreshToken = new RefreshToken
      {
        AspNetUsersId = user.Id,
        Token = new RefreshTokenGenerator().GenerateRefreshToken(),
        ExpiryDate = DateTime.UtcNow.AddHours(1),
      };

      appDbContext.RefreshToken.Add(refreshToken);

      await appDbContext.SaveChangesAsync();

      authenticatedResult.RefreshToken = refreshToken.Token;
    }

    authenticatedResult = await SetAuthenticatedResult(authenticatedResult, user, expires);

    return authenticatedResult;
  }

  private AppUser GetUserFromAccessToken(string token)
  {
    var tokenHandler = new JwtSecurityTokenHandler();

    var tokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
      ValidateIssuer = false,
      ValidateAudience = false,
      RequireAudience = false,
      RequireExpirationTime = false,
      ValidateLifetime = true,
    };

    SecurityToken? securityToken = null;
    JwtSecurityToken? jwtSecurityToken = null;
    AppUser? user = null;

    try
    {
      tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
    }
    catch (Exception)
    {
      // Dummy
    }
    finally
    {
      if (securityToken != null)
      {
        jwtSecurityToken = securityToken as JwtSecurityToken;
      }

      if (jwtSecurityToken != null && jwtSecurityToken.Header.Alg == "HS256")
      {
        var userId = jwtSecurityToken.Claims.Where(x => x.Type == "Id").FirstOrDefault();

        user = appDbContext.AppUser.Where(x => x.Id == userId!.Value).FirstOrDefault();
      }
    }

    return user!;
  }

  private async Task<AuthenticatedResult> SetAuthenticatedResult(AuthenticatedResult authenticatedResult, AppUser user, DateTime expires)
  {
    authenticatedResult.Token = await tokenService.CreateToken(user, expires);
    authenticatedResult.Success = true;
    authenticatedResult.Expires = expires;
    authenticatedResult.UserName = user.UserName!;
    authenticatedResult.FirstName = user.FirstName;
    authenticatedResult.Name = user.LastName;
    authenticatedResult.Id = user.Id;
    authenticatedResult.IsAdmin = await userManager.IsInRoleAsync(user, "Admin");
    authenticatedResult.IsAuthorised = await userManager.IsInRoleAsync(user, "Authorised");

    return authenticatedResult;
  }

  private DateTime SetExpires()
  {
    return DateTime.UtcNow.AddMinutes(15);
  }
}
