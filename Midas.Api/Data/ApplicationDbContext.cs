namespace Midas.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.ApplyConfiguration(new ApplicationUserConfiguration());
		builder.ApplyConfiguration(new PostConfiguration());
		builder.ApplyConfiguration(new CommentConfiguration());
		builder.ApplyConfiguration(new ReactConfiguration());
		builder.ApplyConfiguration(new FollowConfiguration());
		builder.ApplyConfiguration(new NotificationConfiguration());
		builder.ApplyConfiguration(new RefreshTokenConfiguration());
		builder.ApplyConfiguration(new SessionConfiguration());

		builder.Entity<IdentityRole<Guid>>().HasData(
			new IdentityRole<Guid>
			{
				Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
				Name = "User",
				NormalizedName = "USER",
				ConcurrencyStamp = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
			},
			new IdentityRole<Guid>
			{
				Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
				Name = "Admin",
				NormalizedName = "ADMIN",
				ConcurrencyStamp = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
			});
	}
	public DbSet<Post> Posts { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<React> Reacts { get; set; }
	public DbSet<Notification> Notifications { get; set; }
	public DbSet<Follow> Follows { get; set; }
	public DbSet<RefreshToken> RefreshTokens { get; set; }
	public DbSet<Session> Sessions { get; set; }
}
