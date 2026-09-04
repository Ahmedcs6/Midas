namespace Midas.Api.Interfaces;

public interface IPostService
{
	Task<ServiceResult<PostResponse>> CreatePostAsync(CreatePostRequest request);
	Task<ServiceResult> EditPostAsync(int id, EditPostRequest request);
}
