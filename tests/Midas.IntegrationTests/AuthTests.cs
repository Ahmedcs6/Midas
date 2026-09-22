using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.Auth.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class AuthTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	private static LoginRequest CreateLoginRequest(string email = "ahmed_test6@example.com", string password = "Ahmed_cs6")
	{
		return new LoginRequest
		{
			Email = email,
			Client = 0,
			Password = password
		};
	}

	private async Task<string> LoginAndGetRefreshTokenAsync(HttpClient client)
	{
		var (_, refreshToken) = await LoginAsMainAsync(client);
		return refreshToken;
	}

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
			Password = "Ahmed_cs7"
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
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(JsonOptions);
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
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(JsonOptions);
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
			var refreshResponse = await client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequest { RefreshToken = oldRefreshToken });
			Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
			var result = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(JsonOptions);
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
		var refreshResponse = await client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequest { RefreshToken = oldRefreshToken! });
		Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
		var reuseResponse = await client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequest { RefreshToken = oldRefreshToken! });
		Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
	}
}
