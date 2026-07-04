using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Maidas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IIdentityEmailService identityEmailService, IAuthService authService, IEmailSender emailSender) : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
	private readonly IIdentityEmailService _identityEmailService = identityEmailService;
	private readonly IAuthService _authService = authService;
	private readonly IEmailSender _emailSender = emailSender;

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto model)
	{
		var result = await _authService.RegisterAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors.ToList());
		return Ok("Register Succeeded.");
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null || await _userManager.IsEmailConfirmedAsync(user))
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
		var result = await _authService.LoginAsync(model);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors);
		}
		return Ok(result);
	}
	[HttpPost("refresh")]
	public async Task<IActionResult> RefreshToken(RefreshTokenRequest model)
	{
		var result = await _authService.Refresh(model);
		return Ok(result);
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


