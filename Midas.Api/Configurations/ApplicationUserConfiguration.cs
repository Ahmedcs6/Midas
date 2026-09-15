using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Midas.Api.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
	public void Configure(EntityTypeBuilder<ApplicationUser> builder)
	{
		builder.Property(u => u.FirstName)
				.HasMaxLength(50)
				.IsRequired();
		builder.Property(u => u.LastName)
				.HasMaxLength(50)
				.IsRequired();
		builder.OwnsOne(u => u.Address, owned =>
		{
			owned.Property(a => a.Country).HasMaxLength(100);
			owned.Property(a => a.State).HasMaxLength(100);
			owned.Property(a => a.City).HasMaxLength(100);
			owned.Property(a => a.Street).HasMaxLength(200);
		});
	}
}
