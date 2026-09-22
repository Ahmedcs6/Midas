using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.User.Request;
using Midas.Api.Models.Dtos.User.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class UserTests(CustomWebApplicationFactory factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task GetUser_Should_Return_Ok()
	{
		using var client = CreateClient();
		await TestHelpers.EnsureUserAsync(Factory.Services);
		var response = await client.GetAsync("/api/Users/Ahmed_cs6_test");
		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>(JsonOptions);
		Assert.NotNull(result);
		Assert.NotNull(result.Data);
		await using var scope = Factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var expected = await db.Users
			.AsNoTracking()
			.Where(u => u.UserName == "Ahmed_cs6_test")
			.Select(u => new UserResponse
			{
				FirstName = u.FirstName,
				LastName = u.LastName,
				UserName = u.UserName!,
				Gender = u.Gender,
				About = u.About,
				Address = u.Address,
				BirthDate = u.BirthDate,
				ImageUrl = u.ImageUrl,
				FollowersNumber = db.Follows.Count(f => f.FollowingId == u.Id),
				FollowingNumber = db.Follows.Count(f => f.FollowerId == u.Id)
			})
			.SingleOrDefaultAsync();
		Assert.Equivalent(expected, result.Data);
	}

	[Fact]
	public async Task EditUser_Should_Return_Ok()
	{
		using var client = CreateClient();
		EditUserRequest model = new()
		{
			About = Guid.NewGuid().ToString(),
			Address = new()
			{
				Country = "Egypt",
				State = "Qena",
				City = "AbuTesht",
				Street = "Almostaamara"
			},
			BirthDate = DateOnly.Parse("6-12-2005")
		};
		var (accessToken, _) = await LoginAsMainAsync(client);
		var request = new HttpRequestMessage(HttpMethod.Patch, "/api/Users/me")
		{
			Content = JsonContent.Create(model)
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		var response = await client.SendAsync(request);
		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		await using var scope = Factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var expected = await db.Users
			.AsNoTracking()
			.Where(u => u.UserName == "Ahmed_cs6_test")
			.Select(u => new EditUserRequest
			{
				About = u.About,
				Address = u.Address,
				BirthDate = u.BirthDate,
			})
			.SingleOrDefaultAsync();
		Assert.Equivalent(expected, model);
	}
}
