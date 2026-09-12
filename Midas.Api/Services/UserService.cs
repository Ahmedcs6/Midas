namespace Midas.Api.Services;

public class UserService(UserManager<ApplicationUser> userManager, IFileStorage fileStorage, ICurrentUser currentUser, ApplicationDbContext context) : IUserService
{
	public async Task<Result> EditAsync(Guid userId, EditUserRequest request)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.NotFound,
				Message = "User not found."
			};
		}
		user.FirstName = request.FirstName ?? user.FirstName;
		user.LastName = request.LastName ?? user.LastName;
		user.About = request.About ?? user.About;
		user.Address = request.Address ?? user.Address;
		user.BirthDate = request.BirthDate ?? user.BirthDate;

		var result = await userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.Validation,
				Message = string.Join(", ", result.Errors.Select(e => e.Description))
			};
		}

		return new()
		{
			Success = true
		};
	}

	public async Task<Result> EditAvatarAsync(Guid userId, EditAvatarRequest request)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());

		if (user is null)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.NotFound,
				Message = "User not found."
			};
		}
		var oldImage = user.ImageUrl;
		var fileName = await fileStorage.SaveAsync(request.Image, "Avatars");
		user.ImageUrl = fileName;
		var result = await userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			await fileStorage.DeleteAsync($"Avatars/{fileName}");
			return new()
			{
				Success = false,
				Error = ErrorType.Validation,
				Message = string.Join(", ", result.Errors.Select(e => e.Description))
			};
		}
		if (oldImage is not null)
			await fileStorage.DeleteAsync($"Avatars/{oldImage}");
		return new()
		{
			Success = true
		};
	}
	public async Task<Result> Follow(string userName)
	{
		var me = await userManager.FindByIdAsync(currentUser.UserId.ToString()!);
		var user = await userManager.FindByNameAsync(userName);
		if (user is null)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.NotFound,
				Message = "User not found."
			};
		}
		if (user.Id == me!.Id)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.Validation,
				Message = "You cannot follow yourself."
			};
		}
		var follow = new Follow
		{
			FollowerId = me.Id,
			FollowingId = user.Id
		};
		context.Follows.Add(follow);
		try
		{
			await context.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.Conflict,
				Message = "You are already following this user."
			};
		}
		return new()
		{
			Success = true
		};
	}

	public async Task<Result> Unfollow(string userName)
	{
		var user = await userManager.FindByNameAsync(userName);

		if (user is null)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.NotFound,
				Message = "User not found."
			};
		}

		var result = await context.Follows
			.Where(f =>
				f.FollowerId == currentUser.UserId &&
				f.FollowingId == user.Id)
			.ExecuteDeleteAsync();

		if (result == 0)
		{
			return new()
			{
				Success = false,
				Error = ErrorType.NotFound,
				Message = "Follow relationship not found."
			};
		}

		return new()
		{
			Success = true
		};
	}

	public async Task<Result<UserResponse>> GetByUserNameAsync(string userName)
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
		{
			return new()
			{
				Success = false,
				Error = ErrorType.NotFound,
				Message = "User not found."
			};
		}
		return new()
		{
			Success = true,
			Data = user
		};
	}
}
