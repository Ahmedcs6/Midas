using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;

namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAccountService accountService, IJwtService jwtService) : ControllerBase
{
	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequest model)
	{
		var result = await accountService.RegisterAsync(model);
		if (!result.Succeeded)
			return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail("Register Error", result.Errors));
		await accountService.SendConfirmEmailAsync(new() { Email = model.Email });
		return StatusCode(StatusCodes.Status201Created, ResponseHelper.Success(result.User, "Register Succeeded, please confirm your email."));
	}
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest model)
	{
		var result = await accountService.LoginAsync(model);
		if (!result.Succeeded)
		{
			if (result.Errors.Contains("Please confirm your email."))
			{
				return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail("Please confirm your email.", result.Errors));
			}
			if (result.Errors.Contains("Invalid email or password."))
			{
				return Unauthorized(ResponseHelper.Fail("Invalid email or password.", result.Errors));
			}
		}
		return Ok(ResponseHelper.Success(result.RefreshTokenResponse, "Login Succeeded."));
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailRequset model)
	{
		await accountService.SendConfirmEmailAsync(new() { Email = model.Email });
		return Ok(ResponseHelper.Success(new { }));
	}
	[HttpPost("confirm-email")]
	public async Task<IActionResult> ConfirmEmail(
	[FromQuery] string userId,
	[FromQuery] string token)
	{
		var result = await accountService.ConfirmEmailAsync(userId, token);
		if (!result.Succeeded)
		{
			return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail("Invalid data.", result.Errors));
		}
		return Ok(ResponseHelper.Success<object>(new { }));
	}
	[HttpPost("refresh")]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
	{
		var result = await jwtService.RefreshAsync(model);
		if (!result.Succeeded)
			return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail("Invalid data.", result.Errors));
		return Ok(ResponseHelper.Success(result.RefreshTokenResponse));
	}
	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
	{
		var result = await accountService.ForgotPasswordAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors);
		return Ok();
	}
	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
	{
		var result = await accountService.ResetPasswordAsync(model);
		if (!result.Succeeded)
			return BadRequest(result.Errors);
		return Ok();
	}
}
