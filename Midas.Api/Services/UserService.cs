namespace Midas.Api.Services;

public class UserService(UserManager<ApplicationUser> userManager, IFileStorage fileStorage) : IUserService
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly IFileStorage _fileStorage = fileStorage;

	public async Task<bool> EditAsync(string userId, EditUserRequest request)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null) return false;
		user.FirstName = request.FirstName;
		user.LastName = request.LastName;
		user.About = request.About;
		user.Address = request.Address;
		user.BirthDate = request.BirthDate;
		await _userManager.UpdateAsync(user);
		return true;
	}

	public async Task<bool> EditAvatarAsync(string userId, EditAvatarRequest request)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user!.ImageUrl is not null) await _fileStorage.DeleteAsync($"Avatars/{user.ImageUrl}");
		var fileName = await _fileStorage.SaveAsync(request.Image, "Avatars");
		user!.ImageUrl = fileName;
		return (await _userManager.UpdateAsync(user)).Succeeded;
	}

	public async Task<UserResponse?> GetByUserNameAsync(string userName)
	{
		ApplicationUser? user = await _userManager.FindByNameAsync(userName);
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
