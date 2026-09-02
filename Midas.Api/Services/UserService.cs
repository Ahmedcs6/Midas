namespace Midas.Api.Services;

public class UserService(UserManager<ApplicationUser> userManager, IFileStorage fileStorage, ICurrentUser currentUser, ApplicationDbContext context) : IUserService
{
	public async Task<bool> EditAsync(Guid userId, EditUserRequest request)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null) return false;
		user.FirstName = request.FirstName;
		user.LastName = request.LastName;
		user.About = request.About;
		user.Address = request.Address;
		user.BirthDate = request.BirthDate;
		var result = await userManager.UpdateAsync(user);
		return result.Succeeded;
	}

	public async Task<bool> EditAvatarAsync(Guid userId, EditAvatarRequest request)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		var oldimg = user!.ImageUrl;
		var fileName = await fileStorage.SaveAsync(request.Image, "Avatars");
		user!.ImageUrl = fileName;
		var result = await userManager.UpdateAsync(user);
		if (oldimg is not null) await fileStorage.DeleteAsync($"Avatars/{oldimg}");
		return result.Succeeded;
	}

	public async Task<bool> Follow(string userName)
	{
		var me = await userManager.FindByIdAsync(currentUser.UserId.ToString());
		var user = await userManager.FindByNameAsync(userName);
		if (user is null)
			return false;
		if (user.Id == me!.Id)
			return false;
		var follow = new Follow
		{
			FollowerId = me!.Id,
			FollowingId = user.Id
		};
		context.Follows.Add(follow);
		await context.SaveChangesAsync();
		return true;
	}
	public async Task<bool> Unfollow(string userName)
	{
		var user = await userManager.FindByNameAsync(userName);
		if (user is null)
			return false;
		var result = await context.Follows.Where(f => f.FollowerId == currentUser.UserId && f.FollowingId == user.Id).ExecuteDeleteAsync();
		return result > 0;
	}

	public async Task<UserResponse?> GetByUserNameAsync(string userName)
	{
		return await context.Users
		.AsNoTracking()
		.Where(u => u.UserName == userName)
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
			FollowersNumber = context.Follows.Count(f => f.FollowingId == u.Id),
			FollowingNumber = context.Follows.Count(f => f.FollowerId == u.Id)
		})
		.SingleOrDefaultAsync();
	}
}
