using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Midas.Api.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.HasKey(rt => rt.Id);

		builder.Property(rt => rt.Id)
			.ValueGeneratedOnAdd()
			.HasDefaultValueSql("NEWID()");

		builder.Property(rt => rt.TokenHash)
			.IsRequired()
			.HasMaxLength(450);

		builder.Property(rt => rt.ExpiresAt)
			.IsRequired();

		builder.Property(rt => rt.RevokedAt);

		builder.HasIndex(rt => rt.TokenHash)
			.IsUnique();

		builder.HasIndex(rt => rt.SessionId);

		builder.HasIndex(rt => rt.ExpiresAt);

		builder.HasOne(rt => rt.Session)
			.WithMany(s => s.RefreshTokens)
			.HasForeignKey(rt => rt.SessionId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
