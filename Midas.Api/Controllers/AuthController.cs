using Microsoft.AspNetCore.Mvc;

namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAccountService accountService, IJwtService jwtService) : ControllerBase
{
	private readonly IAccountService _accountService = accountService;
	private readonly IJwtService _jwtService = jwtService;

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto model)
	{
		var result = await _accountService.RegisterAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors.ToList());
		await _accountService.SendConfirmEmailAsync(new()
		{
			Email = model.Email
		});
		return Ok("Register Succeeded, please confirm your email.");
	}
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto model)
	{
		var result = await _accountService.LoginAsync(model);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors);
		}
		return Ok(result);
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailDto model)
	{
		await _accountService.SendConfirmEmailAsync(model);
		return Ok();
	}
	[HttpPost("confirm-email")]
	public async Task<IActionResult> ConfirmEmail(
	[FromQuery] string userId,
	[FromQuery] string token)
	{
		var result = await _accountService.ConfirmEmailAsync(userId, token);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors);
		}
		return Ok();
	}
	[HttpPost("refresh")]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
	{
		var result = await _jwtService.RefreshAsync(model);
		if (result.Succeeded == false)
			return BadRequest(result);
		return Ok(result);
	}
	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
	{
		var result = await _accountService.ForgotPasswordAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors);
		return Ok();
	}
	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
	{
		var result = await _accountService.ResetPasswordAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors);
		return Ok();
	}
}
