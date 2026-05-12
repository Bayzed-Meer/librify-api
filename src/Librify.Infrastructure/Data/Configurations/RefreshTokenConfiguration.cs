using Librify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librify.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.HasIndex(rt => rt.FamilyId);
        builder.Property(rt => rt.Token).HasMaxLength(32).IsRequired();
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
