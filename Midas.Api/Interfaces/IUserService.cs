namespace Midas.Api.Interfaces;

public interface IUserService
{
	Task<ServiceResult<UserResponse>> GetByUserNameAsync(string userName);
	Task<ServiceResult> EditAsync(Guid userId, EditUserRequest request);
	Task<ServiceResult> EditAvatarAsync(Guid userId, EditAvatarRequest request);
	Task<ServiceResult> Follow(string userName);
	Task<ServiceResult> Unfollow(string userName);
}
