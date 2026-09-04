using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Auth.Request;
using Midas.Api.Models.Dtos.Auth.Response;
using Midas.Api.Models.Dtos.User.Request;
using Midas.Api.Models.Dtos.User.Response;

namespace Midas.Tests;

[Collection("Api collection")]
public class UserTests(CustomWebApplicationFactory factory)
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
	private static string? _accessToken { get; set; }
	private async Task LoginAsync()
	{
		var loginRequest = CreateLoginRequest();
		var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);
		var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>(_jsonSerializerOptions);
		_accessToken = loginResult!.Data!.AccessToken;
	}
	[Fact]
	public async Task GetUser_Should_Return_Ok()
	{
		var response = await _client.GetAsync("api/Users/Ahmed_cs6_test");
		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>(_jsonSerializerOptions);
		Assert.NotNull(result);
		Assert.NotNull(result.Data);
		await using var scope = _factory.Services.CreateAsyncScope();
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
		var request = new HttpRequestMessage(HttpMethod.Patch, "api/Users/me")
		{
			Content = JsonContent.Create(model)
		};
		if (_accessToken is null)
			await LoginAsync();
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

		var response = await _client.SendAsync(request);
		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		await using var scope = _factory.Services.CreateAsyncScope();
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
