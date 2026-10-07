using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.User.Response;

namespace Midas.IntegrationTests;

[Collection("Api collection")]
public class SessionTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	private async Task<List<SessionResponse>> GetSessionsAsync(HttpClient client, string accessToken)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, "/api/Users/me/sessions");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		var response = await client.SendAsync(request);
		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SessionResponse>>>(JsonOptions);
		Assert.NotNull(result);
		Assert.NotNull(result.Data);
		return result.Data;
	}

	[Fact]
	public async Task ListSessions_Should_ShowActiveSessions_WithCurrentFlag()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "sess");
		var (token1, _) = await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);
		await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);

		var sessions = await GetSessionsAsync(client, token1);

		Assert.Equal(2, sessions.Count);
		Assert.Single(sessions.Where(s => s.IsCurrent));
	}

	[Fact]
	public async Task RevokeOneSession_Should_RemoveIt_And_BreakItsRefresh()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "sessone");
		var (token1, _) = await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);
		var (_, refresh2) = await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);

		var sessions = await GetSessionsAsync(client, token1);
		var other = sessions.Single(s => !s.IsCurrent);

		var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/Users/me/sessions/{other.Id}");
		delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token1);
		var deleteResponse = await client.SendAsync(delete);
		Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

		var remaining = await GetSessionsAsync(client, token1);
		Assert.Single(remaining);

		var refreshResponse = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = refresh2 }
		);
		Assert.Equal(HttpStatusCode.BadRequest, refreshResponse.StatusCode);
	}

	[Fact]
	public async Task RevokeAllExceptCurrent_Should_KeepCurrentSession()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "sessall");
		var (token1, refresh1) = await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);
		var (_, refresh2) = await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);

		var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/Users/me/sessions");
		delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token1);
		var deleteResponse = await client.SendAsync(delete);
		Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

		var sessions = await GetSessionsAsync(client, token1);
		var current = Assert.Single(sessions);
		Assert.True(current.IsCurrent);

		var oldRefreshResponse = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = refresh2 }
		);
		Assert.Equal(HttpStatusCode.BadRequest, oldRefreshResponse.StatusCode);

		var currentRefreshResponse = await client.PostAsJsonAsync(
			"/api/Auth/refresh",
			new RefreshTokenRequest { RefreshToken = refresh1 }
		);
		Assert.Equal(HttpStatusCode.OK, currentRefreshResponse.StatusCode);
	}

	[Fact]
	public async Task RevokeForeignSession_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var user = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "sessmain");
		var (token1, _) = await LoginAsync(client, user.Email!, TestHelpers.UniqueTestPassword);
		var sessions = await GetSessionsAsync(client, token1);

		var peer = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "sesspeer");
		var (peerToken, _) = await LoginAsync(client, peer.Email!, TestHelpers.UniqueTestPassword);

		var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/Users/me/sessions/{sessions[0].Id}");
		delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", peerToken);
		var response = await client.SendAsync(delete);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Sessions_WithoutAuth_Should_Return_Unauthorized()
	{
		using var client = CreateClient();
		var response = await client.GetAsync("/api/Users/me/sessions");
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
