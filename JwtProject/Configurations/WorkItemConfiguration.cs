

using JwtProject.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JwtProject.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("WorkItem");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired();

        builder.Property(t => t.Description);

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(t => t.CreatedById)
            .IsRequired();

        builder.HasOne(t => t.CreatedBy)
               .WithMany()
               .HasForeignKey(t => t.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}