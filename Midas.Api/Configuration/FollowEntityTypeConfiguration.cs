using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Midas.Api.Configuration;

public class FollowEntityTypeConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follow");
        builder.HasKey(f => new { f.FollowerId, f.FollowingId });

        builder.HasOne(f => f.Follower)
            .WithMany(u => u.Followings)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(f => f.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
