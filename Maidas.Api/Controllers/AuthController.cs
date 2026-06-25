using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Maidas.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IIdentityEmailService identityEmailService) : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
	private readonly IIdentityEmailService _identityEmailService = identityEmailService;

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto model)
	{
		ApplicationUser user = new()
		{
			FirstName = model.FirstName,
			LastName = model.LastName,
			UserName = model.UserName,
			Gender = model.Gender,
			Email = model.Email
		};
		IdentityResult result = await _userManager.CreateAsync(user, model.Password);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors);
		}
		await _identityEmailService.SendConfirmationEmailAsync(user);
		return Ok();
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null)
		{
			return Ok();
		}
		if (await _userManager.IsEmailConfirmedAsync(user))
		{
			return Ok();
		}
		await _identityEmailService.SendConfirmationEmailAsync(user);
		return Ok();
	}
	[HttpPost("confirm-email")]
	public async Task<IActionResult> ConfirmEmail(
	[FromQuery] string userId,
	[FromQuery] string token)
	{
		var user = await _userManager.FindByIdAsync(userId);

		if (user is null)
		{
			return BadRequest();
		}

		token = Encoding.UTF8.GetString(
			WebEncoders.Base64UrlDecode(token));

		var result =
			await _userManager.ConfirmEmailAsync(user, token);

		if (!result.Succeeded)
		{
			return BadRequest();
		}

		return Ok();
	}
	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);

		if (user is null)
		{
			return Unauthorized();
		}

		if (!await _userManager.IsEmailConfirmedAsync(user))
		{
			return BadRequest("Email is not confirmed.");
		}

		var result = await _signInManager.PasswordSignInAsync(
			user,
			model.Password,
			isPersistent: false,
			lockoutOnFailure: true);

		if (!result.Succeeded)
		{
			return Unauthorized();
		}

		return Ok();
	}
	[HttpPost("logout")]
	public async Task<IActionResult> Logout()
	{
		await _signInManager.SignOutAsync();
		return Ok();
	}
	[Authorize]
	[HttpDelete("account")]
	public async Task<IActionResult> Delete(DeleteAccountDto model)
	{
		var user = await _userManager.GetUserAsync(User);
		if (user is null)
			return Unauthorized();

		bool validPassword =
			await _userManager.CheckPasswordAsync(user, model.Password);

		if (!validPassword)
			return BadRequest("Invalid password.");

		var result = await _userManager.DeleteAsync(user);

		if (!result.Succeeded)
			return BadRequest(result.Errors);

		await _signInManager.SignOutAsync();

		return NoContent();
	}
}


