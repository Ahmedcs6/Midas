using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos;
using Midas.Api.Models.Dtos.Post.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class PostTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	private async Task<string> LoginAsPeerAsync(HttpClient client)
	{
		var peer = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "postpeer");
		var (accessToken, _) = await LoginAsync(client, peer.Email!, TestHelpers.UniqueTestPassword);
		return accessToken;
	}

	// NOTE: EditPostRequest contains IFormFile, so model binding infers [FromForm].
	// Edits must be sent as multipart/form-data; JSON bodies are silently ignored (no-op 200).
	private static HttpRequestMessage AuthenticatedPatch(string url, string accessToken, string? content = null)
	{
		var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
		var form = new MultipartFormDataContent();
		if (content is not null)
			form.Add(new StringContent(content), "content");
		request.Content = form;
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		return request;
	}

	private async Task<int> GetPostIdByContentAsync(HttpClient client, string userName, string content, string? accessToken = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Users/{userName}/posts?limit=50");
		if (accessToken is not null)
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		var response = await client.SendAsync(request);
		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginationResult<PostResponse, int>>>(JsonOptions);
		Assert.NotNull(result);
		Assert.NotNull(result.Data);
		return result.Data.Items.Single(p => p.Content == content).Id;
	}

	[Fact]
	public async Task CreatePost_WithoutAuth_Should_Return_Unauthorized()
	{
		using var client = CreateClient();
		var response = await client.PostAsJsonAsync("/api/Posts", new { content = "x", privacy = "Public" });
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Create_Edit_Delete_Post_Lifecycle_Should_Work()
	{
		using var client = CreateClient();
		var (token, _) = await LoginAsMainAsync(client);
		var content = $"lifecycle_{Guid.NewGuid():N}";

		var create = await client.SendAsync(Authenticated(HttpMethod.Post, "/api/Posts", token,
			new { content, privacy = "Public" }));
		Assert.Equal(HttpStatusCode.Created, create.StatusCode);
		var created = await create.Content.ReadFromJsonAsync<ApiResponse<PostResponse>>(JsonOptions);
		Assert.NotNull(created);
		Assert.True(created.Success);
		Assert.NotNull(created.Data);
		Assert.Equal(content, created.Data.Content);

		var postId = await GetPostIdByContentAsync(client, TestHelpers.TestUserName, content, token);

		var editedContent = $"edited_{Guid.NewGuid():N}";
		var edit = await client.SendAsync(AuthenticatedPatch($"/api/Posts/{postId}", token, editedContent));
		Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
		var editedId = await GetPostIdByContentAsync(client, TestHelpers.TestUserName, editedContent, token);
		Assert.Equal(postId, editedId);

		var delete = await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Posts/{postId}", token));
		Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

		var afterDelete = await client.SendAsync(Authenticated(HttpMethod.Get, $"/api/Users/{TestHelpers.TestUserName}/posts?limit=50", token));
		var afterResult = await afterDelete.Content.ReadFromJsonAsync<ApiResponse<PaginationResult<PostResponse, int>>>(JsonOptions);
		Assert.NotNull(afterResult);
		Assert.NotNull(afterResult.Data);
		Assert.DoesNotContain(afterResult.Data.Items, p => p.Id == postId);
	}

	[Fact]
	public async Task EditPost_UnknownId_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var (token, _) = await LoginAsMainAsync(client);
		var response = await client.SendAsync(AuthenticatedPatch("/api/Posts/2147483647", token, "ghost"));
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task DeletePost_UnknownId_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var (token, _) = await LoginAsMainAsync(client);
		var response = await client.SendAsync(Authenticated(HttpMethod.Delete, "/api/Posts/2147483647", token));
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task EditPost_OfAnotherUser_Should_Return_Forbidden()
	{
		using var client = CreateClient();
		var (mainToken, _) = await LoginAsMainAsync(client);
		var content = $"peer-target_{Guid.NewGuid():N}";
		var create = await client.SendAsync(Authenticated(HttpMethod.Post, "/api/Posts", mainToken,
			new { content, privacy = "Public" }));
		Assert.Equal(HttpStatusCode.Created, create.StatusCode);
		var postId = await GetPostIdByContentAsync(client, TestHelpers.TestUserName, content, mainToken);

		try
		{
			var peerToken = await LoginAsPeerAsync(client);
			var edit = await client.SendAsync(AuthenticatedPatch($"/api/Posts/{postId}", peerToken, "hijacked"));
			Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
		}
		finally
		{
			await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Posts/{postId}", mainToken));
		}
	}

	[Fact]
	public async Task DeletePost_OfAnotherUser_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var (mainToken, _) = await LoginAsMainAsync(client);
		var content = $"peer-delete-target_{Guid.NewGuid():N}";
		var create = await client.SendAsync(Authenticated(HttpMethod.Post, "/api/Posts", mainToken,
			new { content, privacy = "Public" }));
		Assert.Equal(HttpStatusCode.Created, create.StatusCode);
		var postId = await GetPostIdByContentAsync(client, TestHelpers.TestUserName, content, mainToken);

		try
		{
			var peerToken = await LoginAsPeerAsync(client);
			var delete = await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Posts/{postId}", peerToken));
			Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
		}
		finally
		{
			await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Posts/{postId}", mainToken));
		}
	}

	[Fact]
	public async Task GetPosts_UnknownUser_Should_Return_NotFound()
	{
		using var client = CreateClient();
		var response = await client.GetAsync($"/api/Users/ghost_{Guid.NewGuid():N}/posts");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task PrivatePost_Should_Be_Hidden_From_Other_Users()
	{
		using var client = CreateClient();
		var (mainToken, _) = await LoginAsMainAsync(client);
		var peer = await TestHelpers.EnsureUniqueUserAsync(Factory.Services, "postpeer");
		var (peerToken, _) = await LoginAsync(client, peer.Email!, TestHelpers.UniqueTestPassword);
		var publicContent = $"public_{Guid.NewGuid():N}";
		var privateContent = $"private_{Guid.NewGuid():N}";

		foreach (var (content, privacy) in new[] { (publicContent, "Public"), (privateContent, "Private") })
		{
			var create = await client.SendAsync(Authenticated(HttpMethod.Post, "/api/Posts", mainToken,
				new { content, privacy }));
			Assert.Equal(HttpStatusCode.Created, create.StatusCode);
		}

		var mainPublicId = await GetPostIdByContentAsync(client, TestHelpers.TestUserName, publicContent, mainToken);
		var mainPrivateId = await GetPostIdByContentAsync(client, TestHelpers.TestUserName, privateContent, mainToken);

		try
		{
			var peerView = await client.SendAsync(Authenticated(HttpMethod.Get, $"/api/Users/{TestHelpers.TestUserName}/posts?limit=50", peerToken));
			var peerResult = await peerView.Content.ReadFromJsonAsync<ApiResponse<PaginationResult<PostResponse, int>>>(JsonOptions);
			Assert.NotNull(peerResult);
			Assert.NotNull(peerResult.Data);
			Assert.Contains(peerResult.Data.Items, p => p.Content == publicContent);
			Assert.DoesNotContain(peerResult.Data.Items, p => p.Content == privateContent);
		}
		finally
		{
			await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Posts/{mainPublicId}", mainToken));
			await client.SendAsync(Authenticated(HttpMethod.Delete, $"/api/Posts/{mainPrivateId}", mainToken));
		}
	}
}
