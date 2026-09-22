using System.Net;
using System.Net.Http.Json;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.User.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class UserEdgeCaseTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task GetUser_Unknown_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var response = await client.GetAsync($"/api/Users/ghost_{Guid.NewGuid():N}");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task EditUser_WithoutAuth_Should_Return_Unauthorized()
	{
		using var client = CreateClient();
		var response = await client.PatchAsync("/api/Users/me", JsonContent.Create(new { about = "x" }));
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Follow_UnknownUser_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var (token, _) = await LoginAsMainAsync(client);
		var response = await client.SendAsync(
			Authenticated(HttpMethod.Post, $"/api/Users/follow/ghost_{Guid.NewGuid():N}", token));
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Follow_WithoutAuth_Should_Return_Unauthorized()
	{
		using var client = CreateClient();
		var response = await client.PostAsync($"/api/Users/follow/{TestHelpers.TestUserName}", null);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Follow_Self_Should_Return_BadRequest()
	{
		using var client = CreateClient();
		var (token, _) = await LoginAsMainAsync(client);
		var response = await client.SendAsync(
			Authenticated(HttpMethod.Post, $"/api/Users/follow/{TestHelpers.TestUserName}", token));
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Follow_Unfollow_Lifecycle_Should_Track_Counts()
	{
		using var client = CreateClient();
		// Unique peer per test: counts always start at 0, no cross-test pollution.
		var peerUser = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "peer");
		var peerUserName = peerUser.UserName!;
		var (token, _) = await LoginAsMainAsync(client);

		// Ensure clean slate: unfollow if a previous run left the relation behind.
		await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Users/follow/{peerUserName}", token));

		var follow = await client.SendAsync(
			Authenticated(HttpMethod.Post, $"/api/Users/follow/{peerUserName}", token));
		Assert.Equal(HttpStatusCode.OK, follow.StatusCode);

		var duplicate = await client.SendAsync(
			Authenticated(HttpMethod.Post, $"/api/Users/follow/{peerUserName}", token));
		Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

		var peer = await client.GetFromJsonAsync<ApiResponse<UserResponse>>(
			$"/api/Users/{peerUserName}", JsonOptions);
		Assert.NotNull(peer);
		Assert.NotNull(peer.Data);
		Assert.Equal(1, peer.Data.FollowersNumber);

		var unfollow = await client.SendAsync(
			Authenticated(HttpMethod.Delete, $"/api/Users/follow/{peerUserName}", token));
		Assert.Equal(HttpStatusCode.NoContent, unfollow.StatusCode);

		var peerAfter = await client.GetFromJsonAsync<ApiResponse<UserResponse>>(
			$"/api/Users/{peerUserName}", JsonOptions);
		Assert.NotNull(peerAfter);
		Assert.NotNull(peerAfter.Data);
		Assert.Equal(0, peerAfter.Data.FollowersNumber);

		var unfollowAgain = await client.SendAsync(
			Authenticated(HttpMethod.Delete, $"/api/Users/follow/{peerUserName}", token));
		Assert.Equal(HttpStatusCode.NotFound, unfollowAgain.StatusCode);
	}

	[Fact]
	public async Task Unfollow_UnknownUser_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var (token, _) = await LoginAsMainAsync(client);
		var response = await client.SendAsync(
			Authenticated(HttpMethod.Delete, $"/api/Users/follow/ghost_{Guid.NewGuid():N}", token));
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}
