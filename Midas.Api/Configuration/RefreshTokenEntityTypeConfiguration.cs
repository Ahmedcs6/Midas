using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Midas.Api.Configuration;

public class RefreshTokenEntityTypeConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.HasKey(r => r.Id);

		builder.Property(r => r.TokenHash)
			.IsRequired();

		builder.HasIndex(r => r.TokenHash)
			.IsUnique();

		builder.Property(r => r.Client)
			.IsRequired();

		builder.Property(r => r.ExpiresAt)
			.IsRequired();

		builder.HasOne(r => r.User)
			.WithMany(u => u.RefreshTokens)
			.HasForeignKey(r => r.ApplicationUserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Ignore(r => r.IsExpired);

		builder.Ignore(r => r.IsActive);
	}
}
