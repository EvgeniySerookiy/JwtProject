using JwtProject.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JwtProject.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();
        
        builder.Property(u => u.Role)
            .IsRequired();
        
        builder.Property(u => u.RefreshToken);
        
        builder.Property(u => u.RefreshTokenExpiryTime);
    }
}