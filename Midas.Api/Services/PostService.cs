namespace Midas.Api.Services;

public class PostService(ApplicationDbContext context, IFileStorage fileStorage, ICurrentUser currentUser) : IPostService
{
	public async Task<ServiceResult<PostResponse>> CreatePostAsync(CreatePostRequest request)
	{
		string? fileName = null;
		if (request.Image is not null)
			fileName = await fileStorage.SaveAsync(request.Image, "Posts");
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
			State = ServiceState.Success,
			Data = new()
			{
				Content = post.Content,
				ImageUrl = post.ImageUrl,
				Privacy = post.Privacy.ToString(),
				PublishDate = post.PublishDate
			}
		};
	}
	public async Task<ServiceResult> EditPostAsync(int id, EditPostRequest request)
	{
		var post = await context.Posts.FindAsync(id);
		if (post is null)
			return new() { State = ServiceState.NotFound, Message = "Post not found." };
		if (post.UserId != currentUser.UserId)
			return new() { State = ServiceState.Forbidden, Message = "You cannot edit this post." };
		if (request.RemoveImage)
		{
			await fileStorage.DeleteAsync($"Posts/{post.ImageUrl}");
			post.ImageUrl = null;
		}
		if (request.Content is not null)
			post.Content = request.Content;
		if (request.Image is not null)
		{
			if (post.ImageUrl is not null)
				await fileStorage.DeleteAsync($"Posts/{post.ImageUrl}");
			post.ImageUrl = await fileStorage.SaveAsync(request.Image, "Posts");
		}
		await context.SaveChangesAsync();
		return new() { State = ServiceState.Success };
	}
}
