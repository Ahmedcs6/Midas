namespace Midas.Api.Services;

public class PostService(ApplicationDbContext context, IFileStorage fileStorage, ICurrentUser currentUser) : IPostService
{
	public async Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest request)
	{
		string? fileName = null;
		if (request.Image is not null)
			fileName = await fileStorage.SaveAsync(request.Image, "Posts");
		var post = new Post
		{
			ImageUrl = fileName,
			Content = request.Content,
			Privacy = request.Privacy,
			UserId = (Guid)currentUser.UserId!
		};
		context.Posts.Add(post);
		await context.SaveChangesAsync();
		return new()
		{
			Success = true,
			Data = new()
			{
				Content = post.Content,
				ImageUrl = post.ImageUrl,
				Privacy = post.Privacy.ToString(),
				PublishDate = post.PublishDate
			}
		};
	}
	public async Task<Result> EditPostAsync(int id, EditPostRequest request)
	{
		var post = await context.Posts.FindAsync(id);
		if (post is null)
			return new() { Success = false, Error = Error.NotFound, Message = "Post not found." };
		if (post.UserId != currentUser.UserId)
			return new() { Success = false, Error = Error.AccessDenied, Message = "You cannot edit this post." };
		if (request.RemoveImage && post.ImageUrl is not null)
		{
			await fileStorage.DeleteAsync($"Posts/{post.ImageUrl}");
			post.ImageUrl = null;
		}
		if (request.Content is not null)
			post.Content = request.Content;
		if (request.Image is not null)
		{
			var oldImage = post.ImageUrl;
			post.ImageUrl = await fileStorage.SaveAsync(request.Image, "Posts");
			if (oldImage is not null)
				await fileStorage.DeleteAsync($"Posts/{oldImage}");
		}
		await context.SaveChangesAsync();
		return new() { Success = true };
	}
	public async Task<Result> DeletePostAsync(int id)
	{
		var post = await context.Posts
			.Where(p => p.Id == id && p.UserId == currentUser.UserId)
			.SingleOrDefaultAsync();
		if (post is null)
			return new() { Success = false, Error = Error.NotFound, Message = "Post not found." };

		var imageUrl = post.ImageUrl;
		context.Posts.Remove(post);
		await context.SaveChangesAsync();

		if (imageUrl is not null)
			await fileStorage.DeleteAsync($"Posts/{imageUrl}");

		return new() { Success = true };
	}
	public async Task<Result<PaginationResult<PostResponse, int>>> GetPostsAsync(string userName, int limit, int? cursor)
	{
		const int MaxLimit = 50;
		if (limit < 1 || limit > MaxLimit)
			return new() { Success = false, Error = Error.Validation, Message = $"Limit must be between 1 and {MaxLimit}." };
		if (cursor is not null && cursor <= 0)
			return new() { Success = false, Error = Error.Validation, Message = "Cursor must be a positive post id." };

		var userId = await context.Users.Where(u => u.UserName == userName).Select(u => (Guid?)u.Id).SingleOrDefaultAsync();
		if (userId is null)
			return new() { Success = false, Error = Error.NotFound, Message = "User Name not found." };

		List<Privacy> permissions = [Privacy.Public];

		var isMe = currentUser.UserId == userId;
		if (isMe)
			permissions.AddRange([Privacy.Friends, Privacy.Private]);

		bool isFollowing = false;
		if (currentUser.UserId is not null && !isMe)
			isFollowing = await context.Follows.AnyAsync(f => f.FollowerId == currentUser.UserId && f.FollowingId == userId);
		if (isFollowing)
			permissions.Add(Privacy.Friends);
		var query = context.Posts.AsNoTracking().Where(p => p.User.UserName == userName);
		if (cursor is not null)
			query = query.Where(p => p.Id < cursor);
		var posts = await query.Where(p => permissions.Contains(p.Privacy))
			.OrderByDescending(p => p.Id)
			.Take(limit + 1)
			.Select(p => new PostResponse()
			{
				Id = p.Id,
				Content = p.Content,
				ImageUrl = p.ImageUrl,
				Privacy = p.Privacy.ToString(),
				PublishDate = p.PublishDate
			})
			.ToListAsync();
		var hasNext = posts.Count > limit;
		if (hasNext)
			posts.RemoveAt(posts.Count - 1);

		return new()
		{
			Success = true,
			Data = new()
			{
				Items = posts,
				NextCursor = hasNext ? posts[^1].Id : null
			}
		};
	}
}
