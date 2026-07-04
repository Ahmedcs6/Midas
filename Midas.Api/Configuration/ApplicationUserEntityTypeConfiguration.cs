using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Midas.Api.Configuration;

public class ApplicationUserEntityTypeConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
                .HasMaxLength(50)
                .IsRequired();
        builder.Property(u => u.LastName)
                .HasMaxLength(50)
                .IsRequired();
        builder.OwnsOne(u => u.Address);
    }
}
