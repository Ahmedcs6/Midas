using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Models;

namespace Midas.Tests;

public static class TestHelpers
{
	public const string TestEmail = "ahmed_test6@example.com";
	public const string TestUserName = "Ahmed_cs6_test";
	public const string TestPassword = "Ahmed_cs6";
	public const string UniqueTestPassword = "Test_123aA!";

	public static string UniqueUserName(string prefix)
	{
		var name = $"{prefix}_{Guid.NewGuid():N}";
		return name.Length <= 32 ? name : name.Substring(0, 32);
	}

	public static string UniqueEmail(string prefix)
		=> $"{prefix}_{Guid.NewGuid():N}@example.com";

	public static Task<ApplicationUser> EnsureUniqueUserAsync(
		IServiceProvider services,
		string prefix = "u",
		string password = UniqueTestPassword)
		=> EnsureUserAsync(services, UniqueEmail(prefix), UniqueUserName(prefix), password);

	public static async Task<ApplicationUser> EnsureUserAsync(
		IServiceProvider services,
		string email = TestEmail,
		string userName = TestUserName,
		string password = TestPassword)
	{
		using var scope = services.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

		var user = await userManager.FindByEmailAsync(email);
		if (user is not null)
			return user;

		user = new ApplicationUser
		{
			FirstName = "Ahmed",
			LastName = "Mahmoud",
			UserName = userName,
			Email = email,
			EmailConfirmed = false
		};

		var result = await userManager.CreateAsync(user, password);
		if (!result.Succeeded)
			throw new InvalidOperationException(
				"Test user creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

		result = await userManager.AddToRoleAsync(user, "User");
		if (!result.Succeeded)
			throw new InvalidOperationException(
				"Test user role assignment failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

		var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
		result = await userManager.ConfirmEmailAsync(user, token);
		if (!result.Succeeded)
			throw new InvalidOperationException(
				"Test user email confirmation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

		return user;
	}
}
