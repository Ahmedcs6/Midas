namespace Midas.Api.Services;

public class UserService(UserManager<ApplicationUser> userManager, IFileStorage fileStorage, ICurrentUser currentUser, ApplicationDbContext context) : IUserService
{
	public async Task<ServiceResult> EditAsync(Guid userId, EditUserRequest request)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null)
			return new() { State = ServiceState.NotFound, Message = "User not found." };

		user.FirstName = request.FirstName;
		user.LastName = request.LastName;
		user.About = request.About;
		user.Address = request.Address;
		user.BirthDate = request.BirthDate;

		var result = await userManager.UpdateAsync(user);

		if (!result.Succeeded)
			return new() { State = ServiceState.BadRequest, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };

		return new() { State = ServiceState.Success };
	}

	public async Task<ServiceResult> EditAvatarAsync(Guid userId, EditAvatarRequest request)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null)
			return new() { State = ServiceState.NotFound, Message = "User not found." };

		var oldimg = user.ImageUrl;
		var fileName = await fileStorage.SaveAsync(request.Image, "Avatars");
		user.ImageUrl = fileName;

		var result = await userManager.UpdateAsync(user);

		if (oldimg is not null)
			await fileStorage.DeleteAsync($"Avatars/{oldimg}");

		if (!result.Succeeded)
			return new() { State = ServiceState.BadRequest, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };

		return new() { State = ServiceState.Success };
	}

	public async Task<ServiceResult> Follow(string userName)
	{
		var me = await userManager.FindByIdAsync(currentUser.UserId.ToString());
		var user = await userManager.FindByNameAsync(userName);

		if (user is null)
			return new() { State = ServiceState.NotFound, Message = "User not found." };

		if (user.Id == me!.Id)
			return new() { State = ServiceState.BadRequest, Message = "You cannot follow yourself." };

		var follow = new Follow
		{
			FollowerId = me!.Id,
			FollowingId = user.Id
		};

		context.Follows.Add(follow);
		await context.SaveChangesAsync();

		return new() { State = ServiceState.Success };
	}

	public async Task<ServiceResult> Unfollow(string userName)
	{
		var user = await userManager.FindByNameAsync(userName);

		if (user is null)
			return new() { State = ServiceState.NotFound, Message = "User not found." };

		var result = await context.Follows
			.Where(f => f.FollowerId == currentUser.UserId && f.FollowingId == user.Id)
			.ExecuteDeleteAsync();

		if (result == 0)
			return new() { State = ServiceState.NotFound, Message = "Follow relationship not found." };

		return new() { State = ServiceState.Success };
	}

	public async Task<ServiceResult<UserResponse>> GetByUserNameAsync(string userName)
	{
		var user = await context.Users
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

		if (user is null)
			return new() { State = ServiceState.NotFound, Message = "User not found." };

		return new() { State = ServiceState.Success, Data = user };
	}
}
