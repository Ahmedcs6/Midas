using Microsoft.AspNetCore.Mvc;

namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
	private readonly IAuthService _authService = authService;

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto model)
	{
		var result = await _authService.RegisterAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors.ToList());
		await _authService.SendConfirmEmailAsync(new()
		{
			Email = model.Email
		});
		return Ok("Register Succeeded, please confirm your email.");
	}
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto model)
	{
		var result = await _authService.LoginAsync(model);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors);
		}
		return Ok(result);
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailDto model)
	{
		await _authService.SendConfirmEmailAsync(model);
		return Ok();
	}
	[HttpPost("confirm-email")]
	public async Task<IActionResult> ConfirmEmail(
	[FromQuery] string userId,
	[FromQuery] string token)
	{
		var result = await _authService.ConfirmEmailAsync(userId, token);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors);
		}
		return Ok();
	}
	[HttpPost("refresh")]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
	{
		var result = await _authService.RefreshAsync(model);
		if (result.Succeeded == false)
			return BadRequest(result);
		return Ok(result);
	}
	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
	{
		var result = await _authService.ForgotPasswordAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors);
		return Ok();
	}
	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
	{
		var result = await _authService.ResetPasswordAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors);
		return Ok();
	}
}
