namespace Midas.Api.Interfaces;

public interface IPostService
{
	Task<ServiceResult<PostResponse>> CreatePostAsync(CreatePostRequest request);
	Task<ServiceResult> EditPostAsync(int id, EditPostRequest request);
	Task<ServiceResult> DeletePostAsync(int id);
	Task<ServiceResult<PaginationResult<PostResponse, int>>> GetPostsAsync(string userName, int limit, int? cursor);
}
