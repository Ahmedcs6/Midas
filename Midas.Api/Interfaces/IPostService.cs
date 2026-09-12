namespace Midas.Api.Interfaces;

public interface IPostService
{
	Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest request);
	Task<Result> EditPostAsync(int id, EditPostRequest request);
	Task<Result> DeletePostAsync(int id);
	Task<Result<PaginationResult<PostResponse, int>>> GetPostsAsync(string userName, int limit, int? cursor);
}
