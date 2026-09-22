using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.Auth.Response;

namespace Midas.Tests;

/// <summary>
/// Shared integration-test plumbing. Derived classes keep [Collection("Api collection")].
/// Each test creates its own HttpClient (per-test lifetime) and logs in with fresh tokens.
/// Prefer unique users per test; legacy shared user helpers remain for login-only tests.
/// </summary>
public abstract class ApiTestBase(CustomWebApplicationFactory factory)
{
	protected readonly CustomWebApplicationFactory Factory = factory;

	protected static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() }
	};

	protected HttpClient CreateClient() => Factory.CreateClient();

	protected static HttpRequestMessage Authenticated(HttpMethod method, string url, string accessToken, object? body = null)
	{
		var request = new HttpRequestMessage(method, url);
		if (body is not null)
			request.Content = JsonContent.Create(body);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		return request;
	}

	protected async Task<(string AccessToken, string RefreshToken)> LoginAsync(
		HttpClient client, string email, string password)
	{
		var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest
		{
			Email = email,
			Password = password,
			Client = 0
		});
		loginResponse.EnsureSuccessStatusCode();
		var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(JsonOptions);
		Assert.NotNull(result);
		Assert.True(result.Success);
		Assert.NotNull(result.Data);
		return (result.Data.AccessToken, result.Data.RefreshToken);
	}

	/// <summary>Login as the legacy shared user (created on demand).</summary>
	protected async Task<(string AccessToken, string RefreshToken)> LoginAsMainAsync(HttpClient client)
	{
		await TestHelpers.EnsureUserAsync(Factory.Services);
		return await LoginAsync(client, TestHelpers.TestEmail, TestHelpers.TestPassword);
	}
}
