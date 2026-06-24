using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Maidas.Api.Configuration;

public class ReactEntityTypeConfiguration : IEntityTypeConfiguration<React>
{
    public void Configure(EntityTypeBuilder<React> builder)
    {
        builder.ToTable("React");
        builder
            .HasKey(r => new { r.UserId, r.PostId });
        builder
            .HasOne(r => r.Post)
            .WithMany(p => p.Reacts)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(r => r.User)
            .WithMany(u => u.Reacts)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
