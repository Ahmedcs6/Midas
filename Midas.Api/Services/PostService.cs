using Midas.Api.Models.Dtos.Post.Request;
using Midas.Api.Models.Dtos.Post.Response;

namespace Midas.Api.Services;

public class PostService(ApplicationDbContext context, IFileStorage fileStorage, ICurrentUser currentUser) : IPostService
{
	public async Task<PostResponse> CreatePost(CreatePostRequest request)
	{
		var fileName = await fileStorage.SaveAsync(request.Image, "Posts");
		var post = new Post
		{
			ImageUrl = fileName,
			Content = request.Content,
			Privacy = request.Privacy,
			UserId = currentUser.UserId
		};
		context.Posts.Add(post);
		await context.SaveChangesAsync();
		return new()
		{
			Content = post.Content,
			ImageUrl = post.ImageUrl,
			Privacy = post.Privacy.ToString(),
			PublishDate = post.PublishDate
		};
	}
}
