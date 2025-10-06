using Microsoft.EntityFrameworkCore;
using TestAutomationApp.Shared.Models;

namespace TestAutomationApp.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<TestScript> TestScripts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(32);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(32);
            entity.Property(e => e.EmailAddress).IsRequired();
            entity.Property(e => e.OfficeBureau).IsRequired();
        });

        modelBuilder.Entity<TestScript>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UiDescription).IsRequired();
            entity.Property(e => e.TestFramework).IsRequired();
            entity.Property(e => e.GeneratedScript).IsRequired();
        });
    }
}
