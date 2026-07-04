namespace Maidas.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.ApplyConfiguration(new ApplicationUserEntityTypeConfiguration());
		builder.ApplyConfiguration(new PostEntityTypeConfiguration());
		builder.ApplyConfiguration(new CommentEntityTypeConfiguration());
		builder.ApplyConfiguration(new ReactEntityTypeConfiguration());
		builder.ApplyConfiguration(new FollowEntityTypeConfiguration());
		builder.ApplyConfiguration(new NotificationEntityTypeConfiguration());
	}
	public DbSet<Post> Posts { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<React> Reacts { get; set; }
	public DbSet<Notification> Notifications { get; set; }
	public DbSet<Follow> Follows { get; set; }
	public DbSet<RefreshToken> RefreshTokens { get; set; }
}
