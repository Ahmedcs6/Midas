namespace Midas.Api.Interfaces;

public interface IUserService
{
	Task<Result<UserResponse>> GetByUserNameAsync(string userName);
	Task<Result> EditAsync(Guid userId, EditUserRequest request);
	Task<Result> EditAvatarAsync(Guid userId, EditAvatarRequest request);
	Task<Result> Follow(string userName);
	Task<Result> Unfollow(string userName);
}
