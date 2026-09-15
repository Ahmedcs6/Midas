using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Midas.Api.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
	public void Configure(EntityTypeBuilder<Session> builder)
	{
		builder.HasKey(s => s.Id);

		builder.Property(s => s.Id)
			.ValueGeneratedOnAdd()
			.HasDefaultValueSql("NEWID()");

		builder.Property(s => s.Client)
			.IsRequired();

		builder.Property(s => s.OperatingSystem)
			.HasMaxLength(100);

		builder.Property(s => s.Browser)
			.HasMaxLength(100);

		builder.Property(s => s.Device)
			.HasMaxLength(100);

		builder.Property(s => s.IpAddress)
			.HasMaxLength(45);

		builder.Property(s => s.CreatedAt)
			.IsRequired()
			.HasDefaultValueSql("GETUTCDATE()");

		builder.Property(s => s.LastActivityAt)
			.IsRequired()
			.HasDefaultValueSql("GETUTCDATE()");

		builder.HasOne(s => s.User)
			.WithMany(u => u.Sessions)
			.HasForeignKey(s => s.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(s => s.RefreshTokens)
			.WithOne(rt => rt.Session)
			.HasForeignKey(rt => rt.SessionId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(s => s.UserId);

		builder.HasIndex(s => new { s.UserId, s.RevokedAt });
	}
}
