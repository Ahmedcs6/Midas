using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.Auth.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class AuthEdgeCaseTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task Register_Duplicate_Should_Return_Conflict()
	{
		using var client = CreateClient();
		var request = new RegisterRequest
		{
			FirstName = "Dup",
			LastName = "Test",
			Gender = 0,
			UserName = TestHelpers.UniqueUserName("Dup"),
			Email = TestHelpers.UniqueEmail("dup_test"),
			Password = "Dup_test1"
		};

		var first = await client.PostAsJsonAsync("/api/Auth/register", request);
		Assert.Equal(HttpStatusCode.Created, first.StatusCode);

		var second = await client.PostAsJsonAsync("/api/Auth/register", request);
		Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
	}

	[Fact]
	public async Task Register_InvalidEmail_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var request = new RegisterRequest
		{
			FirstName = "Bad",
			LastName = "Email",
			Gender = 0,
			UserName = TestHelpers.UniqueUserName("BadEmail"),
			Email = "not-an-email",
			Password = "Bad_test1"
		};
		var response = await client.PostAsJsonAsync("/api/Auth/register", request);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Register_MissingPassword_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var payload = new
		{
			firstName = "No",
			lastName = "Password",
			gender = "Male",
			userName = TestHelpers.UniqueUserName("NoPass"),
			email = TestHelpers.UniqueEmail("nopass_test")
		};
		var response = await client.PostAsJsonAsync("/api/Auth/register", payload);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Login_UnconfirmedEmail_Should_Return_Forbidden()
	{
		using var client = CreateClient();
		var email = TestHelpers.UniqueEmail("unconfirmed_test");
		var userName = TestHelpers.UniqueUserName("Unconfirmed");

		var register = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
		{
			FirstName = "Unconfirmed",
			LastName = "Test",
			Gender = 0,
			UserName = userName,
			Email = email,
			Password = "Unconfirmed_1"
		});
		Assert.Equal(HttpStatusCode.Created, register.StatusCode);

		var login = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest
		{
			Email = email,
			Password = "Unconfirmed_1",
			Client = 0
		});
		Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
	}

	[Fact]
	public async Task Login_MissingFields_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync("/api/Auth/login", new { });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Refresh_MalformedToken_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = "not-valid-base64!!!" });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(JsonOptions);
		Assert.NotNull(result);
		Assert.False(result.Success);
	}

	[Fact]
	public async Task Refresh_UnknownToken_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var unknown = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
		var response = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = unknown });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Refresh_EmptyToken_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = string.Empty });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task ConfirmEmail_MalformedToken_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		await TestHelpers.EnsureUserAsync(Factory.Services);
		await using var scope = Factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var userId = await db.Users
			.Where(u => u.Email == TestHelpers.TestEmail)
			.Select(u => u.Id)
			.SingleAsync();

		var response = await client.PostAsync(
			$"/api/Auth/confirm-email?userId={userId}&token=bad-token!!!",
			null);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task ConfirmEmail_UnknownUser_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var response = await client.PostAsync(
			$"/api/Auth/confirm-email?userId={Guid.NewGuid()}&token=AAAAAAAAAAAAAAAA",
			null);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task ResendConfirmEmail_UnknownEmail_Should_Return_Ok()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/resend-confirm-email",
			new { email = $"ghost_{Guid.NewGuid():N}@example.com" });
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task ForgotPassword_UnknownEmail_Should_Return_Ok()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/forgot-password",
			new { email = $"ghost_{Guid.NewGuid():N}@example.com" });
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task ForgotPassword_InvalidEmail_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/forgot-password",
			new { email = "not-an-email" });
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}
}
