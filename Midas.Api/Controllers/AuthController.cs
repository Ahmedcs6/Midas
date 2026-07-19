using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;

namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAccountService accountService, IJwtService jwtService, IServiceScopeFactory scopeFactory) : ControllerBase
{
	private readonly IAccountService _accountService = accountService;
	private readonly IJwtService _jwtService = jwtService;
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto model)
	{
		var sw = Stopwatch.StartNew();
		var result = await _accountService.RegisterAsync(model);
		Console.WriteLine($"Register: {sw.ElapsedMilliseconds} ms");
		if (!result.Succeeded)
			return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail<object>("Register Error", result.Errors));
		sw.Restart();
		_ = Task.Run(async () =>
		{
			using var scope = _scopeFactory.CreateScope();
			var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
			await accountService.SendConfirmEmailAsync(new() { Email = model.Email });
		});
		Console.WriteLine($"Send Email: {sw.ElapsedMilliseconds} ms");
		return StatusCode(StatusCodes.Status201Created, ResponseHelper.Success(result.User, "Register Succeeded, please confirm your email."));
	}
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto model)
	{
		var sw = Stopwatch.StartNew();
		var result = await _accountService.LoginAsync(model);
		if (!result.Succeeded)
		{
			if (result.Errors.Contains("Please confirm your email."))
			{
				return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail<object>("Please confirm your email.", result.Errors));
			}
			if (result.Errors.Contains("Invalid email or password."))
			{
				return Unauthorized(ResponseHelper.Fail<object>("Invalid email or password.", result.Errors));
			}
		}
		Console.WriteLine($"Login: {sw.ElapsedMilliseconds} ms");
		return Ok(ResponseHelper.Success(result.RefreshTokenResponse, "Login Succeeded."));
	}
	[HttpPost("resend-confirm-email")]
	public async Task<IActionResult> ResendConfirmEmail([FromBody] ConfirmEmailDto model)
	{
		await _accountService.SendConfirmEmailAsync(model);
		return Ok(ResponseHelper.Success(new { }));
	}
	[HttpPost("confirm-email")]
	public async Task<IActionResult> ConfirmEmail(
	[FromQuery] string userId,
	[FromQuery] string token)
	{
		var result = await _accountService.ConfirmEmailAsync(userId, token);
		if (!result.Succeeded)
		{
			return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail<object>("Invalid data.", result.Errors));
		}
		return Ok(ResponseHelper.Success<object>(new { }));
	}
	[HttpPost("refresh")]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
	{
		var result = await _jwtService.RefreshAsync(model);
		if (!result.Succeeded)
			return StatusCode(StatusCodes.Status403Forbidden, ResponseHelper.Fail<object>("Invalid data.", result.Errors));
		return Ok(ResponseHelper.Success(result.RefreshTokenResponse));
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
