using Microsoft.AspNetCore.Mvc;

namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAccountService accountService, IJwtService jwtService) : ControllerBase
{
	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request)
	{
		var result = await accountService.RegisterAsync(request);
		return this.ToActionResult(result, StatusCodes.Status201Created);
	}
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		var result = await accountService.LoginAsync(request);
		return this.ToActionResult(result);
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailRequset request)
	{
		var result = await accountService.SendConfirmEmailAsync(request);
		return this.ToActionResult(result);
	}
	[HttpPost("confirm-email")]
	public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
	{
		var result = await accountService.ConfirmEmailAsync(userId, token);
		return this.ToActionResult(result);
	}
	[HttpPost("refresh")]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
	{
		var result = await jwtService.RefreshAsync(request);
		return this.ToActionResult(result);
	}
	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
	{
		var result = await accountService.ForgotPasswordAsync(request);
		return this.ToActionResult(result);
	}
	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
	{
		var result = await accountService.ResetPasswordAsync(request);
		return this.ToActionResult(result);
	}
}
