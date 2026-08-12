namespace Midas.Api.Interfaces;

public interface IUserService
{
	Task<UserResponse?> GetByUserNameAsync(string userName);
	Task<bool> EditAsync(string userId, EditUserRequest request);
	Task<bool> EditAvatarAsync(string userId, EditAvatarRequest request);
}
