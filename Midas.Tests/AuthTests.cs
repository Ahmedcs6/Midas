using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Data;

namespace Midas.Tests;

[Collection("Api collection")]
public class AuthTests(CustomWebApplicationFactory factory)
{
	private readonly CustomWebApplicationFactory _factory = factory;
	private readonly HttpClient _client = factory.CreateClient();

	[Fact]
	public async Task Register_Should_Return_Created()
	{
		using var scope = _factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await db.Users.ExecuteDeleteAsync();
		var request = new
		{
			firstName = "Ahmed",
			lastName = "Mahmoud",
			gender = 0,
			userName = "Ahmed_cs6_test",
			email = "ahmed_test@example.com",
			password = "Ahmed_cs6"
		};

		var response = await _client.PostAsJsonAsync(
			"/api/Auth/register",
			request);

		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(
			HttpStatusCode.Created,
			response.StatusCode);
		using var verifyScope = _factory.Services.CreateAsyncScope();

		var verifyDb =
			verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var user = await verifyDb.Users
			.SingleOrDefaultAsync(x => x.UserName == request.userName);

		Assert.NotNull(user);
		Assert.Equal(request.email, user.Email);
		Assert.Equal(request.firstName, user.FirstName);
		Assert.Equal(request.lastName, user.LastName);
	}
}
