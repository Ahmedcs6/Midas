using Midas.Api.Models.Dtos.Post.Request;
using Midas.Api.Models.Dtos.Post.Response;

namespace Midas.Api.Interfaces;

public interface IPostService
{
	Task<PostResponse> CreatePost(CreatePostRequest request);
}
