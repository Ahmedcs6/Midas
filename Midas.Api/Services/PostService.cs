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
			return new() { Success = false, Error = ErrorType.NotFound, Message = "Post not found." };
		if (post.UserId != currentUser.UserId)
			return new() { Success = false, Error = ErrorType.AccessDenied, Message = "You cannot edit this post." };
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
		var result = await context.Posts.Where(p => p.Id == id && p.UserId == currentUser.UserId).ExecuteDeleteAsync();
		if (result <= 0)
			return new() { Success = false, Error = ErrorType.NotFound };
		return new() { Success = true };
	}
	public async Task<Result<PaginationResult<PostResponse, int>>> GetPostsAsync(string userName, int limit, int? cursor)
	{
		var userId = await context.Users.Where(u => u.UserName == userName).Select(u => (Guid?)u.Id).SingleOrDefaultAsync();
		if (userId is null)
			return new() { Success = false, Error = ErrorType.NotFound, Message = "User Name not found." };

		List<PrivacyType> permissions = [PrivacyType.Public];

		var isMe = currentUser.UserId == userId;
		if (isMe)
			permissions.AddRange([PrivacyType.Friends, PrivacyType.Private]);

		bool isFollowing = false;
		if (currentUser.UserId is not null && !isMe)
			isFollowing = await context.Follows.AnyAsync(f => f.FollowerId == currentUser.UserId && f.FollowingId == userId);
		if (isFollowing)
			permissions.Add(PrivacyType.Friends);
		var query = context.Posts.AsNoTracking().Where(p => p.User.UserName == userName);
		if (cursor is not null)
			query = query.Where(p => p.Id < cursor);
		var posts = query.Where(p => permissions.Contains(p.Privacy))
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
			.ToList();
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
