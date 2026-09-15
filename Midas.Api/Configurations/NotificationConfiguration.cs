using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Midas.Api.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder.ToTable("Notification");
		builder.HasKey(n => n.Id);

		builder.Property(n => n.Content)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(n => n.IsRead)
			.HasDefaultValue(false);

		builder.Property(n => n.CreatedAt)
			.HasDefaultValueSql("GETUTCDATE()");

		builder.HasOne(n => n.User)
			.WithMany(u => u.Notifications)
			.HasForeignKey(n => n.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
	}
}
