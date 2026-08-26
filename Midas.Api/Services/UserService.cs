namespace Midas.Api.Services;

public class UserService(UserManager<ApplicationUser> userManager, IFileStorage fileStorage, ICurrentUser currentUser, ApplicationDbContext context) : IUserService
{
	public async Task<bool> EditAsync(string userId, EditUserRequest request)
	{
		var user = await userManager.FindByIdAsync(userId);
		if (user is null) return false;
		user.FirstName = request.FirstName;
		user.LastName = request.LastName;
		user.About = request.About;
		user.Address = request.Address;
		user.BirthDate = request.BirthDate;
		var result = await userManager.UpdateAsync(user);
		return result.Succeeded;
	}

	public async Task<bool> EditAvatarAsync(string userId, EditAvatarRequest request)
	{
		var user = await userManager.FindByIdAsync(userId);
		var oldimg = user!.ImageUrl;
		var fileName = await fileStorage.SaveAsync(request.Image, "Avatars");
		user!.ImageUrl = fileName;
		var result = await userManager.UpdateAsync(user);
		if (oldimg is not null) await fileStorage.DeleteAsync($"Avatars/{oldimg}");
		return result.Succeeded;
	}

	public async Task<bool> Follow(string userName)
	{
		var me = await userManager.FindByIdAsync(currentUser.UserId!);
		var user = await userManager.FindByNameAsync(userName);
		if (user is null)
			return false;
		if (user == me)
			return false;
		var follow = new Follow
		{
			FollowerId = me!.Id,
			FollowingId = user.Id
		};
		await context.Follows.AddAsync(follow);
		await context.SaveChangesAsync();
		return true;
	}

	public async Task<UserResponse?> GetByUserNameAsync(string userName)
	{
		ApplicationUser? user = await userManager.FindByNameAsync(userName);
		if (user is null)
			return null;
		UserResponse response = new()
		{
			FirstName = user.FirstName,
			LastName = user.LastName,
			UserName = user.UserName!,
			Gender = user.Gender,
			About = user.About,
			Address = user.Address,
			BirthDate = user.BirthDate,
			ImageUrl = user.ImageUrl
		};
		return response;
	}
}
