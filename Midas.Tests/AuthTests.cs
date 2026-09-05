using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.Auth.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class AuthTests(CustomWebApplicationFactory factory)
{
	private readonly CustomWebApplicationFactory _factory = factory;
	private readonly HttpClient _client = factory.CreateClient();
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	private static LoginRequest CreateLoginRequest(string email = "ahmed_test6@example.com", string password = "Ahmed_cs6")
	{
		return new LoginRequest
		{
			Email = email,
			Client = 0,
			Password = password

		};
	}
	private static string? _accessToken;
	private static string? _refreshToken;
	private async Task LoginAsync()
	{
		var loginRequest = CreateLoginRequest();
		var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);
		var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(_jsonSerializerOptions);
		_accessToken = loginResult!.Data!.AccessToken;
		_refreshToken = loginResult.Data.RefreshToken;
	}
	[Fact]
	public async Task Registre_Should_Return_Ok()
	{
		using var scope = _factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await db.Users.Where(u => u.UserName == "Ahmed_cs7_test").ExecuteDeleteAsync();
		var request = new RegisterRequest
		{
			FirstName = "Ahmed",
			LastName = "Mahmoud",
			Gender = 0,
			UserName = "Ahmed_cs7_test",
			Email = "ahmed_test7@example.com",
			Password = "Ahmed_cs7"
		};
		var response = await _client.PostAsJsonAsync("/api/Auth/register", request);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		await using var verifyScope = _factory.Services.CreateAsyncScope();
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
		var request = CreateLoginRequest();
		var response = await _client.PostAsJsonAsync("api/Auth/login", request);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(_jsonSerializerOptions);
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
		var request = CreateLoginRequest(password: "mesh hadaf");
		var response = await _client.PostAsJsonAsync("api/Auth/login", request);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(_jsonSerializerOptions);
		Assert.False(result!.Success);
		Assert.Null(result.Data);
	}
	[Fact]
	public async Task RefreshToken_Should_Return_New_Tokens()
	{
		await LoginAsync();
		var oldRefreshToken = _refreshToken;
		for (int i = 0; i < 5; i++)
		{
			var refreshResponse = await _client.PostAsJsonAsync("api/Auth/refresh", new RefreshTokenRequest { RefreshToken = oldRefreshToken });
			Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
			var result = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(_jsonSerializerOptions);
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
		await LoginAsync();
		var oldRefreshToken = _refreshToken;
		var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequest { RefreshToken = oldRefreshToken });
		Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
		var reuseResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequest { RefreshToken = oldRefreshToken });
		Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
	}
}
