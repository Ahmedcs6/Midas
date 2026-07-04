public class CustomUserValidator
	: UserValidator<ApplicationUser>
{
	private static readonly HashSet<string> ReservedNames =
	[
		"me",
		"admin",
		"api"
	];

	public override async Task<IdentityResult> ValidateAsync(
		UserManager<ApplicationUser> manager,
		ApplicationUser user)
	{
		if (ReservedNames.Contains(
			user.UserName!.ToLowerInvariant()))
		{
			return IdentityResult.Failed(
				new IdentityError
				{
					Description = "Username is reserved."
				});
		}

		return IdentityResult.Success;
	}
}
