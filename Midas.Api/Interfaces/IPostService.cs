using Midas.Api.Models.Dtos.Post.Request;
using Midas.Api.Models.Dtos.Post.Response;

namespace Midas.Api.Interfaces;

public interface IPostService
{
	Task<ServiceResult<PostResponse>> CreatePostAsync(CreatePostRequest request);
	Task<ServiceResult> EditPostAsync(int id, EditPostRequest request);
}
