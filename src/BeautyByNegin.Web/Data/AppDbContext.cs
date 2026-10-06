using BeautyByNegin.Web.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Web.Data;

/// <summary>
/// Single EF Core context for the whole site. Provider-agnostic: nothing here is SQLite specific,
/// so switching to SQL Server / PostgreSQL only means changing the provider call in DatabaseSetup.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Language> Languages => Set<Language>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Language>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(5);
            e.Property(x => x.CultureName).HasMaxLength(10).IsRequired();
            e.Property(x => x.NativeName).HasMaxLength(50).IsRequired();
            e.Property(x => x.ShortLabel).HasMaxLength(5).IsRequired();
        });
    }
}
