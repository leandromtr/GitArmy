using GitArmy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitArmy.Infrastructure.Persistence.Configurations;

internal class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Username)
            .HasMaxLength(39)
            .IsRequired();

        builder.HasIndex(p => p.Username).IsUnique();

        builder.HasIndex(p => new { p.Score, p.SubstitutionYear });

        builder.Property(p => p.Score).IsRequired();
        builder.Property(p => p.ProcessedAt).IsRequired();
    }
}
