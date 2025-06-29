using JwtProject.Configurations;
using JwtProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtProject.Data;

public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<WorkItem> WorkItems { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) 
        : base(options) {}
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new WorkItemConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}