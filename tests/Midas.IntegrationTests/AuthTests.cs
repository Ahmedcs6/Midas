using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.Auth.Response;

namespace Midas.IntegrationTests;

/// <summary>
/// All authentication tests in one place:
/// happy path, edge cases, and break-my-code security tests
/// (the latter assert the SECURE behavior — a failure proves a live bug).
/// </summary>
[Collection("Api collection")]
public class AuthTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	private static LoginRequest CreateLoginRequest(
		string email = "ahmed_test6@example.com",
		string password = "Ahmed_cs6"
	)
	{
		return new LoginRequest
		{
			Email = email,
			Client = 0,
			Password = password,
		};
	}

	private async Task<string> LoginAndGetRefreshTokenAsync(HttpClient client)
	{
		var (_, refreshToken) = await LoginAsMainAsync(client);
		return refreshToken;
	}

	#region Happy path

	[Fact]
	public async Task Register_Should_Return_Created()
	{
		using var client = CreateClient();
		var userName = TestHelpers.UniqueUserName("Ahmed_cs");
		var email = TestHelpers.UniqueEmail("ahmed_test");
		var request = new RegisterRequest
		{
			FirstName = "Ahmed",
			LastName = "Mahmoud",
			Gender = 0,
			UserName = userName,
			Email = email,
			Password = "Ahmed_cs7",
		};
		var response = await client.PostAsJsonAsync("/api/Auth/register", request);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		await using var verifyScope = Factory.Services.CreateAsyncScope();
		var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var user = await verifyDb.Users.SingleOrDefaultAsync(x => x.UserName == request.UserName);
		Assert.NotNull(user);
		Assert.Equal(request.Email, user.Email);
		Assert.Equal(request.FirstName, user.FirstName);
		Assert.Equal(request.LastName, user.LastName);
	}

	[Fact]
	public async Task Login_Should_Return_Ok()
	{
		using var client = CreateClient();
		await TestHelpers.EnsureUserAsync(Factory.Services);
		var request = CreateLoginRequest();
		var response = await client.PostAsJsonAsync("/api/Auth/login", request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(
			JsonOptions
		);
		Assert.True(result!.Success);
		Assert.NotNull(result.Data);
		Assert.NotNull(result.Data.AccessToken);
		Assert.NotEmpty(result.Data.AccessToken);
		Assert.NotNull(result.Data.RefreshToken);
		Assert.NotEmpty(result.Data.RefreshToken);
	}

	[Fact]
	public async Task Login_Should_Return_Unauthorized()
	{
		using var client = CreateClient();
		var request = CreateLoginRequest(password: "mesh hadaf");
		var response = await client.PostAsJsonAsync("/api/Auth/login", request);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(
			JsonOptions
		);
		Assert.False(result!.Success);
		Assert.Null(result.Data);
	}

	[Fact]
	public async Task RefreshToken_Should_Return_New_Tokens()
	{
		using var client = CreateClient();
		var oldRefreshToken = await LoginAndGetRefreshTokenAsync(client);
		for (int i = 0; i < 5; i++)
		{
			var refreshResponse = await client.PostAsJsonAsync(
				"/api/Auth/refresh",
				new RefreshTokenRequest { RefreshToken = oldRefreshToken }
			);
			Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
			var result = await refreshResponse.Content.ReadFromJsonAsync<
				ApiResponse<RefreshTokenResponse>
			>(JsonOptions);
			Assert.NotNull(result);
			Assert.NotNull(result.Data);
			Assert.NotEmpty(result.Data.RefreshToken);
			Assert.NotEmpty(result.Data.AccessToken);
			var newToken = result.Data.RefreshToken;
			Assert.NotStrictEqual(newToken, oldRefreshToken);
			oldRefreshToken = newToken;
		}
	}

	[Fact]
	public async Task RefreshToken_Should_Reject_Revoked_Token()
	{
		using var client = CreateClient();
		var oldRefreshToken = await LoginAndGetRefreshTokenAsync(client);
		var refreshResponse = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = oldRefreshToken }
		);
		Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
		var reuseResponse = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = oldRefreshToken }
		);
		Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
	}

	#endregion

	#region Edge cases

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
			Password = "Dup_test1",
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
			Password = "Bad_test1",
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
			email = TestHelpers.UniqueEmail("nopass_test"),
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

		var register = await client.PostAsJsonAsync(
			"/api/Auth/register",
			new RegisterRequest
			{
				FirstName = "Unconfirmed",
				LastName = "Test",
				Gender = 0,
				UserName = userName,
				Email = email,
				Password = "Unconfirmed_1",
			}
		);
		Assert.Equal(HttpStatusCode.Created, register.StatusCode);

		var login = await client.PostAsJsonAsync(
			"/api/Auth/login",
			new LoginRequest
			{
				Email = email,
				Password = "Unconfirmed_1",
				Client = 0,
			}
		);
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
			new RefreshTokenRequest { RefreshToken = "not-valid-base64!!!" }
		);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(
			JsonOptions
		);
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
			new RefreshTokenRequest { RefreshToken = unknown }
		);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Refresh_EmptyToken_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = string.Empty }
		);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task ConfirmEmail_MalformedToken_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		await TestHelpers.EnsureUserAsync(Factory.Services);
		await using var scope = Factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var userId = await db
			.Users.Where(u => u.Email == TestHelpers.TestEmail)
			.Select(u => u.Id)
			.SingleAsync();

		var response = await client.PostAsync(
			$"/api/Auth/confirm-email?userId={userId}&token=bad-token!!!",
			null
		);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task ConfirmEmail_UnknownUser_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var response = await client.PostAsync(
			$"/api/Auth/confirm-email?userId={Guid.NewGuid()}&token=AAAAAAAAAAAAAAAA",
			null
		);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task ResendConfirmEmail_UnknownEmail_Should_Return_Ok()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/resend-confirm-email",
			new { email = $"ghost_{Guid.NewGuid():N}@example.com" }
		);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task ForgotPassword_UnknownEmail_Should_Return_Ok()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/forgot-password",
			new { email = $"ghost_{Guid.NewGuid():N}@example.com" }
		);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task ForgotPassword_InvalidEmail_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/forgot-password",
			new { email = "not-an-email" }
		);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	#endregion

	#region Security (break-my-code)

	[Fact]
	public async Task Refresh_RevokedSession_Should_Fail()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services);
		var (_, refreshToken) = await LoginAsync(
			client,
			user.Email!,
			TestHelpers.UniqueTestPassword
		);

		// Revoke the session but leave the refresh token itself active.
		await using (var scope = Factory.Services.CreateAsyncScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var dbUser = await db.Users.SingleAsync(u => u.Email == user.Email);
			var session = await db.Sessions.FirstAsync(s =>
				s.UserId == dbUser.Id && s.RevokedAt == null
			);
			session.RevokedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		var response = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = refreshToken }
		);

		// Secure behavior: revoked session must be rejected (currently mapped to 400).
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Login_MissingClient_Should_Be_BadRequest()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services);

		// Omit `client` entirely: [Required] on non-nullable enum should reject.
		var response = await client.PostAsJsonAsync(
			"/api/Auth/login",
			new { email = user.Email, password = TestHelpers.UniqueTestPassword }
		);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Register_InvalidGender_Should_Be_BadRequest()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync(
			"/api/Auth/register",
			new
			{
				firstName = "Bad",
				lastName = "Gender",
				gender = 999,
				userName = TestHelpers.UniqueUserName("BadGender"),
				email = TestHelpers.UniqueEmail("badgender_test"),
				password = TestHelpers.UniqueTestPassword,
			}
		);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Login_LockedOut_Should_Return_Locked()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services);

		// First 4 bad attempts: plain 401. The 5th reaches
		// MaxFailedAccessAttempts and locks the account (423).
		for (int i = 0; i < 4; i++)
		{
			var bad = await client.PostAsJsonAsync(
				"/api/Auth/login",
				new LoginRequest
				{
					Email = user.Email!,
					Password = "Wrong_123!",
					Client = 0,
				}
			);
			Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
		}

		var fifth = await client.PostAsJsonAsync(
			"/api/Auth/login",
			new LoginRequest
			{
				Email = user.Email!,
				Password = "Wrong_123!",
				Client = 0,
			}
		);
		Assert.Equal((HttpStatusCode)423, fifth.StatusCode);

		// Correct password while locked: still 423.
		var locked = await client.PostAsJsonAsync(
			"/api/Auth/login",
			new LoginRequest
			{
				Email = user.Email!,
				Password = TestHelpers.UniqueTestPassword,
				Client = 0,
			}
		);

		Assert.Equal((HttpStatusCode)423, locked.StatusCode);
	}

	#endregion
}
