namespace Midas.Api.Interfaces;

public interface IUserService
{
	Task<UserResponse?> GetByUserNameAsync(string userName);
	Task<bool> EditAsync(Guid userId, EditUserRequest request);
	Task<bool> EditAvatarAsync(Guid userId, EditAvatarRequest request);
	Task<bool> Follow(string userName);
	Task<bool> Unfollow(string userName);
}
