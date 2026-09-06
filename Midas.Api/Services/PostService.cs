using System.ComponentModel;
using Org.BouncyCastle.Utilities.IO;

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
	public async Task<ServiceResult> DeletePostAsync(int id)
	{
		var result = await context.Posts.Where(p => p.Id == id && p.UserId == currentUser.UserId).ExecuteDeleteAsync();
		if (result <= 0)
			return new() { State = ServiceState.NotFound };
		return new() { State = ServiceState.Success };
	}
	public async Task<ServiceResult<PaginationResult<PostResponse, int>>> GetPostsAsync(string userName, int limit, int? cursor)
	{
		var userExists = await context.Users.AnyAsync(u => u.UserName == userName);
		if (!userExists)
			return new() { State = ServiceState.NotFound, Message = "User Name not found." };
		var query = context.Posts.AsNoTracking().Where(p => p.User.UserName == userName);
		if (cursor is not null)
			query = query.Where(p => p.Id < cursor);
		var posts = query.OrderByDescending(p => p.Id)
			.Take(limit + 1)
			.Select(p => new PostResponse()
			{
				Id = p.Id,
				Content = p.Content,
				ImageUrl = p.ImageUrl,
				Privacy = p.Privacy.ToString(),
				PublishDate = p.PublishDate
			})
			.ToList();
		var hasNext = posts.Count > limit;
		if (hasNext)
			posts.RemoveAt(posts.Count - 1);

		return new()
		{
			State = ServiceState.Success,
			Data = new()
			{
				Items = posts,
				NextCursor = hasNext ? posts[^1].Id : null
			}
		};
	}
}
